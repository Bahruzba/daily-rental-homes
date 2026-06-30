export type RentalHome = {
  id: number
  title: string
  city: string
  district?: string | null
  address?: string | null
  description: string
  dailyPrice: number
  roomCount: number
  guestCount: number
  isPublished: boolean
  images: string[]
  imageAlt: string
  rating: number
  reviews: number
  amenities: string[]
  contact: {
    name: string
    phone: string
    whatsapp: string
  }
  badge?: string
}

export type ApiResponse<T> = {
  success: boolean
  data?: T
  error?: string
}

export type BookingPayload = {
  rentalHomeId: number
  name: string
  phone: string
  guests: number
  price: number
  dates: string[]
  note?: string
}

export type BookingResult = {
  id: number
  demo: boolean
}
