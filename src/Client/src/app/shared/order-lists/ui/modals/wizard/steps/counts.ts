/** Romanian plural: 1 articol, 2 articole, 20 de articole. */
export function formatArticleCount(count: number): string {
  if (count === 1)
    return '1 articol';

  const lastTwoDigits = count % 100;

  return lastTwoDigits >= 1 && lastTwoDigits < 20 ? `${count} articole` : `${count} de articole`;
}

export function formatListCount(count: number): string {
  return count === 1 ? '1 listă selectată' : `${count} liste selectate`;
}
