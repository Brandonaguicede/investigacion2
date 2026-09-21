type BidFormProps = {
  connected: boolean
  onBid: (bidder: string, amount: number) => void
}

export function BidForm({ connected, onBid }: BidFormProps) {
  return (
    <form className="bid-form" onSubmit={(event) => {
      event.preventDefault()
      const values = new FormData(event.currentTarget)
      const bidder = String(values.get('participant') ?? '').trim()
      const amount = Number(values.get('amount'))
      if (bidder && Number.isFinite(amount) && amount > 0) onBid(bidder, amount)
    }}>
      <div className="field">
        <label htmlFor="participant">Participante</label>
        <input id="participant" name="participant" type="text" maxLength={100} autoComplete="given-name" required />
      </div>
      <div className="field">
        <label htmlFor="amount">Tu oferta</label>
        <input id="amount" name="amount" type="number" min="0.01" step="0.01" inputMode="decimal" aria-describedby="bid-help" required />
      </div>
      <button type="submit" disabled={!connected}>Ofertar</button>
      <p id="bid-help" className="field-help">Disponible cuando exista una conexión con el servidor.</p>
    </form>
  )
}
