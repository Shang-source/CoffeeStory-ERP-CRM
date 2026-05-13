export function MoneyText({ value }: { value: number }) {
  return <>${value.toFixed(2)}</>;
}
