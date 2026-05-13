import { Chip } from '@mui/material';

export function StatusChip({ label, color }: { label: string; color: string }) {
  return <Chip label={label} size="small" sx={{ bgcolor: color, color: 'white' }} />;
}
