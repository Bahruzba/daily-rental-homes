import { useCallback, useEffect, useState } from 'react'

export const comparePropertyStorageKey = 'daily-homes-compare-property-ids'
const comparePropertyEventName = 'daily-homes-compare-property-ids-changed'

function readCompareIds() {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(comparePropertyStorageKey) ?? '[]')
    if (!Array.isArray(parsed)) return []
    return [...new Set(parsed.map(Number).filter((id) => Number.isInteger(id) && id > 0))].slice(0, 3)
  } catch {
    return []
  }
}

function writeCompareIds(ids: number[]) {
  try {
    window.localStorage.setItem(comparePropertyStorageKey, JSON.stringify([...new Set(ids)].slice(0, 3)))
    window.dispatchEvent(new Event(comparePropertyEventName))
  } catch {
    // Compare state still works in-memory for the current page if localStorage is unavailable.
  }
}

export function useCompareProperties() {
  const [compareIds, setCompareIds] = useState<number[]>(readCompareIds)

  useEffect(() => {
    const sync = () => setCompareIds(readCompareIds())
    window.addEventListener('storage', sync)
    window.addEventListener(comparePropertyEventName, sync)
    return () => {
      window.removeEventListener('storage', sync)
      window.removeEventListener(comparePropertyEventName, sync)
    }
  }, [])

  const isCompared = useCallback((id: number) => compareIds.includes(id), [compareIds])

  const setCompared = useCallback((id: number, selected: boolean) => {
    setCompareIds((current) => {
      const next = selected ? [...new Set([...current, id])].slice(0, 3) : current.filter((item) => item !== id)
      writeCompareIds(next)
      return next
    })
  }, [])

  const clearCompare = useCallback(() => {
    setCompareIds([])
    writeCompareIds([])
  }, [])

  return { compareIds, isCompared, setCompared, clearCompare }
}
