import { MessageCircle, Phone } from 'lucide-react'
import type { RentalHome } from '../types'

export function ContactBox({ contact }: { contact: RentalHome['contact'] }) {
  return <section className="broker-card"><div className="broker-avatar">{contact.name.split(' ').map((part) => part[0]).join('').slice(0, 2)}</div><div><span className="eyebrow">BROKER</span><h3>{contact.name}</h3><p>Adətən 15 dəqiqə ərzində cavab verir</p></div><a className="button button-outline" href={contact.whatsapp} target="_blank" rel="noreferrer"><MessageCircle size={17} /> WhatsApp</a><a className="button button-ghost" href={`tel:${contact.phone.replace(/\s/g, '')}`} aria-label="Brokerə zəng et"><Phone size={17} /></a></section>
}
