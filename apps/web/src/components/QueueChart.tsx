import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';

interface QueueData {
  pending: number;
  processing: number;
  completed: number;
  failed: number;
}

export function QueueChart({ data }: { data?: QueueData }) {
  const chartData = data
    ? [
        { name: 'Pending', value: data.pending, color: '#f59e0b' },
        { name: 'Processing', value: data.processing, color: '#3b82f6' },
        { name: 'Completed', value: data.completed, color: '#22c55e' },
        { name: 'Failed', value: data.failed, color: '#ef4444' },
      ]
    : [
        { name: 'Pending', value: 0, color: '#f59e0b' },
        { name: 'Processing', value: 0, color: '#3b82f6' },
        { name: 'Completed', value: 0, color: '#22c55e' },
        { name: 'Failed', value: 0, color: '#ef4444' },
      ];

  return (
    <div className="h-64">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" vertical={false} />
          <XAxis type="number" tick={{ fill: '#6b7280', fontSize: 12 }} axisLine={false} tickLine={false} />
          <YAxis dataKey="name" type="category" tick={{ fill: '#6b7280', fontSize: 12 }} axisLine={false} tickLine={false} width={80} />
          <Tooltip
            contentStyle={{
              backgroundColor: '#1f2937',
              border: 'none',
              borderRadius: '8px',
              color: '#f3f4f6',
            }}
            labelStyle={{ color: '#9ca3af' }}
          />
          <Bar dataKey="value" radius={[0, 4, 4, 0]} maxBarSize={40}>
            {chartData.map((entry, index) => (
              <Cell key={index} fill={entry.color} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}