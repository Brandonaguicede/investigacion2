param([string]$Endpoint)
if (!$Endpoint) { $settings = Get-Content (Join-Path $PSScriptRoot '../backend/WEBSOCKET/Properties/launchSettings.json') -Raw | ConvertFrom-Json; $Endpoint = $settings.profiles.http.applicationUrl.Replace('http://', 'ws://') + '/ws' }

# Ejecutar contra un backend recién iniciado (precio inicial 100).
# Con Docker Compose: .\websocket-integration-test.ps1 -Endpoint ws://localhost:8081/ws
# Cada cliente recibe el estado actual al conectarse; el script lo comprueba.
$ErrorActionPreference = 'Stop'
$deadline = [System.Threading.CancellationTokenSource]::new(60000)
$clients = [System.Collections.Generic.List[System.Net.WebSockets.ClientWebSocket]]::new()

function Connect-Client {
    $client = [System.Net.WebSockets.ClientWebSocket]::new()
    $client.ConnectAsync([uri]$Endpoint, $deadline.Token).GetAwaiter().GetResult() | Out-Null
    $clients.Add($client)
    return $client
}

function Send-Text($Client, [string]$Text, [bool]$End = $true) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $Client.SendAsync([System.ArraySegment[byte]]::new($bytes),
        [System.Net.WebSockets.WebSocketMessageType]::Text, $End, $deadline.Token).GetAwaiter().GetResult() | Out-Null
}

function Start-Receive($Client) {
    $buffer = [byte[]]::new(4096)
    return @{ Buffer = $buffer; Task = $Client.ReceiveAsync([System.ArraySegment[byte]]::new($buffer), $deadline.Token) }
}

function Complete-Receive($Pending) {
    $result = $Pending.Task.GetAwaiter().GetResult()
    if (!$result.EndOfMessage -or $result.MessageType -ne [System.Net.WebSockets.WebSocketMessageType]::Text) {
        throw 'Expected a complete JSON text response'
    }
    return ([System.Text.Encoding]::UTF8.GetString($Pending.Buffer, 0, $result.Count) | ConvertFrom-Json)
}

function Receive-Json($Client) { return (Complete-Receive (Start-Receive $Client)) }
function Assert-Update($Message, $Price, $Winner) {
    if ($Message.type -ne 'auctionUpdated' -or $Message.currentPrice -ne $Price -or $Message.currentWinner -ne $Winner) {
        throw "Unexpected update: $($Message | ConvertTo-Json -Compress)"
    }
}
function Assert-Error($Message) {
    if ($Message.type -ne 'error' -or [string]::IsNullOrWhiteSpace($Message.message)) { throw 'Expected ErrorMessage' }
}

function Send-Bid($Client, $Bidder, $Amount) {
    Send-Text $Client (@{ type = 'placeBid'; bidder = $Bidder; amount = $Amount } | ConvertTo-Json -Compress)
}
function Assert-PrivateError($Sender, $ObserverPending, $Json, $Label) {
    Send-Text $Sender $Json
    $errorMessage = Receive-Json $Sender
    Assert-Error $errorMessage
    if ($ObserverPending.Task.Wait(100)) { throw "Unexpected broadcast: $Label" }
    Write-Output "PASS $Label : $($errorMessage.message)"
}
try {
    Write-Output "Endpoint: $Endpoint; initial state: 100 / no winner / Active"
    $a = Connect-Client
    $b = Connect-Client
    if ($a.State -ne 'Open' -or $b.State -ne 'Open') { throw 'Connections not open' }
    Assert-Update (Receive-Json $a) 100 $null
    Assert-Update (Receive-Json $b) 100 $null
    Write-Output 'PASS A: two open clients; each receives the initial state 100 / no winner without bidding'
    Send-Bid $a 'Kenneth' 120
    Assert-Update (Receive-Json $a) 120 'Kenneth'
    Assert-Update (Receive-Json $b) 120 'Kenneth'
    Write-Output 'PASS B: broadcast 120 / Kenneth to A and B'
    $late = Connect-Client
    Assert-Update (Receive-Json $late) 120 'Kenneth'
    $late.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'late client done', $deadline.Token).GetAwaiter().GetResult() | Out-Null
    Write-Output 'PASS B2: late client receives 120 / Kenneth on connect, without any new bid'
    $pendingA = Start-Receive $a
    Assert-PrivateError $b $pendingA '{"type":"placeBid","bidder":"Maria","amount":110}' 'C lower bid'
    Assert-PrivateError $b $pendingA '{"type":"placeBid","bidder":"Maria","amount":120}' 'D equal bid'
    Send-Bid $b 'Maria' 150
    Assert-Update (Complete-Receive $pendingA) 150 'Maria'
    Assert-Update (Receive-Json $b) 150 'Maria'
    Write-Output 'PASS E: broadcast 150 / Maria to both; no queued rejection broadcast'
    $pendingA = Start-Receive $a
    Assert-PrivateError $b $pendingA '{"type":"placeBid","bidder":"","amount":200}' 'F empty bidder'
    Assert-PrivateError $b $pendingA '{"type":"placeBid","bidder":"Maria","amount":0}' 'G zero amount'
    Assert-PrivateError $b $pendingA '{"type":"placeBid","bidder":"Maria","amount":-10}' 'G negative amount'
    Assert-PrivateError $b $pendingA '{"type":' 'H invalid JSON'
    Assert-PrivateError $b $pendingA '{"type":"somethingElse"}' 'I unsupported type'
    Assert-PrivateError $b $pendingA ('{"type":"placeBid","bidder":"' + ('x' * 101) + '","amount":300}') 'I2 bidder longer than 100 characters'
    Send-Bid $b 'Maria' 151
    Assert-Update (Complete-Receive $pendingA) 151 'Maria'
    Assert-Update (Receive-Json $b) 151 'Maria'
    Write-Output 'PASS F-I recovery: 151 accepted after invalid 200; both clients still usable'
    $b.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'test J', $deadline.Token).GetAwaiter().GetResult() | Out-Null
    Send-Bid $a 'Kenneth' 160
    Assert-Update (Receive-Json $a) 160 'Kenneth'
    Write-Output 'PASS J: B closed; A receives 160 / Kenneth; manager count checked in logs'
    $b = Connect-Client
    Assert-Update (Receive-Json $b) 160 'Kenneth'
    Send-Bid $b 'Maria' 170
    Assert-Update (Receive-Json $a) 170 'Maria'
    Assert-Update (Receive-Json $b) 170 'Maria'
    Write-Output 'PASS K: B reconnected, received 160 / Kenneth on connect and its bid was accepted'
    for ($round = 1; $round -le 20; $round++) {
        $low = 170 + $round * 20
        $high = $low + 10
        $bytesA = [System.Text.Encoding]::UTF8.GetBytes((@{type='placeBid';bidder='Kenneth';amount=$low} | ConvertTo-Json -Compress))
        $bytesB = [System.Text.Encoding]::UTF8.GetBytes((@{type='placeBid';bidder='Maria';amount=$high} | ConvertTo-Json -Compress))
        if ($round % 2 -eq 0) {
            $sendB = $b.SendAsync([System.ArraySegment[byte]]::new($bytesB), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $deadline.Token)
            $sendA = $a.SendAsync([System.ArraySegment[byte]]::new($bytesA), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $deadline.Token)
        } else {
            $sendA = $a.SendAsync([System.ArraySegment[byte]]::new($bytesA), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $deadline.Token)
            $sendB = $b.SendAsync([System.ArraySegment[byte]]::new($bytesB), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $deadline.Token)
        }
        [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($sendA, $sendB))
        foreach ($client in @($a, $b)) {
            $lastPrice = $low - 10
            do {
                $update = Receive-Json $client
                if ($update.type -eq 'error') { Assert-Error $update; continue }
                if ($update.type -ne 'auctionUpdated' -or $update.currentPrice -le $lastPrice) { throw 'Non-monotonic update' }
                $lastPrice = $update.currentPrice
            } while ($lastPrice -ne $high)
            Assert-Update $update $high 'Maria'
        }
        # Marcadores por conexión: drenan rechazos pendientes y detectan regresiones tras el máximo.
        foreach ($client in @($a, $b)) { Send-Text $client '{"type":"barrier"}' }
        foreach ($client in @($a, $b)) {
            do {
                $marker = Receive-Json $client
                Assert-Error $marker
            } while ($marker.message -ne 'Tipo de mensaje no soportado.')
        }
        Write-Output "PASS L round $round : $low vs $high -> $high / Maria, no regression"
    }
    $c = Connect-Client
    Assert-Update (Receive-Json $c) $high 'Maria'
    $b.Abort()
    Send-Bid $a 'Kenneth' 1000
    Assert-Update (Receive-Json $a) 1000 'Kenneth'
    Assert-Update (Receive-Json $c) 1000 'Kenneth'
    Write-Output 'PASS M: aborted B; A and C both receive 1000 / Kenneth'
    foreach ($client in @($a, $c)) {
        $client.CloseAsync([System.Net.WebSockets.WebSocketCloseStatus]::NormalClosure, 'complete', $deadline.Token).GetAwaiter().GetResult() | Out-Null
    }
    Write-Output 'ALL A-M PASSED'
} finally {
    foreach ($client in $clients) { $client.Dispose() }
    $deadline.Dispose()
}
