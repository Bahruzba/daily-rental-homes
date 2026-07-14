import { useCallback, useEffect, useState } from 'react'

export const favoritePropertyStorageKey = 'daily-homes-favorite-property-ids'
const favoritePropertyEventName = 'daily-homes-favorite-property-ids-changed'

function readFavoriteIds() {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(favoritePropertyStorageKey) ?? '[]')
    if (!Array.isArray(parsed)) return []
    return [...new Set(parsed.map(Number).filter((id) => Number.isInteger(id) && id > 0))]
  } catch {
    return []
  }
}

function writeFavoriteIds(ids: number[]) {
  try {
    window.localStorage.setItem(favoritePropertyStorageKey, JSON.stringify([...new Set(ids)]))
    window.dispatchEvent(new Event(favoritePropertyEventName))
  } catch {
    // Favorites remain available for the current page state even if localStorage is blocked.
  }
}

export function useFavoriteProperties() {
  const [favoriteIds, setFavoriteIds] = useState<number[]>(readFavoriteIds)

  useEffect(() => {
    const sync = () => setFavoriteIds(readFavoriteIds())
    window.addEventListener('storage', sync)
    window.addEventListener(favoritePropertyEventName, sync)
    return () => {
      window.removeEventListener('storage', sync)
      window.removeEventListener(favoritePropertyEventName, sync)
    }
  }, [])

  const isFavorite = useCallback((id: number) => favoriteIds.includes(id), [favoriteIds])

  const toggleFavorite = useCallback((id: number) => {
    setFavoriteIds((current) => {
      const next = current.includes(id) ? current.filter((item) => item !== id) : [...current, id]
      writeFavoriteIds(next)
      return next
    })
  }, [])

  return { favoriteIds, isFavorite, toggleFavorite }
}
