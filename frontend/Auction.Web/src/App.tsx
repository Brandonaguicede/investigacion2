import { useEffect, useRef, useState } from 'react'
import { AuctionCard } from './components/AuctionCard'
import { BidForm } from './components/BidForm'
import { ConnectionStatus } from './components/ConnectionStatus'

export default function App() {
  const socket = useRef<WebSocket | null>(null)
  const [status, setStatus] = useState('Conectando')
  const [price, setPrice] = useState<number | null>(null)
  const [winner, setWinner] = useState<string | null>(null)
  const [message, setMessage] = useState('Esperando conexión...')

  useEffect(() => {
    const url = new URL('/ws', window.location.href)
    url.protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:'
    const connection = new WebSocket(url)
    socket.current = connection
    let active = true
    connection.onopen = () => {
      if (!active) return
      setStatus('Conectado')
      setMessage('Conexión establecida. Esperando ofertas.')
    }
    connection.onmessage = (event) => {
      if (!active) return
      try {
        const data: unknown = JSON.parse(event.data)
        if (!data || typeof data !== 'object') throw new Error('Invalid message')
        if ('type' in data && data.type === 'auctionUpdated' &&
            'currentPrice' in data && typeof data.currentPrice === 'number' &&
            'currentWinner' in data &&
            (typeof data.currentWinner === 'string' || data.currentWinner === null)) {
          setPrice(data.currentPrice)
          setWinner(data.currentWinner)
          setMessage('Subasta actualizada.')
        } else if ('type' in data && data.type === 'error' &&
                   'message' in data && typeof data.message === 'string') {
          setMessage(data.message)
        } else {
          setMessage('Mensaje del servidor no reconocido.')
        }
      } catch {
        setMessage('Mensaje del servidor no válido.')
      }
    }
    connection.onerror = () => {
      if (active) setMessage('No se pudo comunicar con el servidor.')
    }
    connection.onclose = () => {
      if (!active) return
      setStatus('Desconectado')
      setMessage('Conexión cerrada. Recarga la página para reconectar.')
    }
    return () => {
      active = false
      socket.current = null
      connection.close()
    }
  }, [])

  function placeBid(bidder: string, amount: number) {
    if (socket.current?.readyState !== WebSocket.OPEN) {
      setMessage('No hay conexión con el servidor.')
      return
    }
    try {
      socket.current.send(JSON.stringify({ type: 'placeBid', bidder, amount }))
    } catch {
      setMessage('No se pudo enviar la oferta.')
    }
  }

  return (
    <main className="auction">
      <header className="page-header">
        <h1>Subasta en tiempo real</h1>
        <ConnectionStatus status={status} />
      </header>
      <AuctionCard currentPrice={price} currentWinner={winner} />
      <BidForm connected={status === 'Conectado'} onBid={placeBid} />
      <section className="messages" aria-labelledby="messages-title">
        <h2 id="messages-title">Mensajes</h2>
        <p role="status">{message}</p>
      </section>
    </main>
  )
}
