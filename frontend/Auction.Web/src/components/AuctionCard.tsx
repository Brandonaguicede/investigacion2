type AuctionCardProps = {
  currentPrice: number | null
  currentWinner: string | null
}

export function AuctionCard({ currentPrice, currentWinner }: AuctionCardProps) {
  return (
    <section className="auction-card" aria-labelledby="price-title">
      <h2 id="price-title">Precio actual</h2>
      <p className="price">{currentPrice === null ? '---' : '$' + currentPrice}</p>
      <dl className="winner">
        <dt>Ganador actual</dt>
        <dd>{currentWinner ?? '---'}</dd>
      </dl>
    </section>
  )
}
