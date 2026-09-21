type ConnectionStatusProps = {
  status: string
}

export function ConnectionStatus({ status }: ConnectionStatusProps) {
  return (
    <p className="connection-status" role="status">
      <span className="status-dot" aria-hidden="true" />
      Estado: <strong>{status}</strong>
    </p>
  )
}
