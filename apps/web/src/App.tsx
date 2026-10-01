import { Routes, Route, Outlet, NavLink, useLocation } from 'react-router-dom';
import { LayoutDashboard, Ticket, Video, CreditCard, Bug, Activity, Settings, Database, Server, AlertTriangle, BarChart3 } from 'lucide-react';
import { clsx } from 'clsx';
import { useState } from 'react';
import Dashboard from './pages/Dashboard';
import HighLoadLab from './pages/HighLoadLab';
import PaymentLab from './pages/PaymentLab';
import FailureInjection from './pages/FailureInjection';
import Observability from './pages/Observability';
import Architecture from './pages/Architecture';
import Documentation from './pages/Documentation';

function Sidebar() {
  const location = useLocation();
  const [collapsed, setCollapsed] = useState(false);

  const navItems = [
    { path: '/', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/high-load', label: 'High Load Lab', icon: Ticket },
    { path: '/streaming', label: 'Streaming Lab', icon: Video },
    { path: '/payments', label: 'Payment Lab', icon: CreditCard },
    { path: '/chaos', label: 'Chaos Engineering', icon: Bug },
    { path: '/observability', label: 'Observability', icon: Activity },
    { path: '/architecture', label: 'Architecture', icon: Database },
    { path: '/docs', label: 'Documentation', icon: BookOpen },
  ];

  return (
    <aside className={clsx(
      'fixed left-0 top-0 h-full bg-gray-900 text-white transition-all duration-300 z-40 border-r border-gray-700',
      collapsed ? 'w-20' : 'w-64'
    )}>
      <div className="flex flex-col h-full">
        <div className={clsx('p-4 border-b border-gray-700 flex items-center justify-between', collapsed && 'justify-center')}>
          {!collapsed && (
            <h1 className="text-xl font-bold text-purple-400 flex items-center gap-2">
              <Server className="w-6 h-6" />
              BackOps Simulator
            </h1>
          )}
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="p-2 rounded-lg hover:bg-gray-800 transition-colors"
            aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {collapsed ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
          </button>
        </div>

        <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
          {navItems.map((item) => {
            const isActive = location.pathname === item.path || (item.path !== '/' && location.pathname.startsWith(item.path));
            return (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) => clsx(
                  'flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors',
                  'text-gray-300 hover:text-white hover:bg-gray-800',
                  isActive && 'bg-purple-900/30 text-purple-300 border-l-2 border-purple-400',
                  collapsed && 'justify-center'
                )}
                title={collapsed ? item.label : undefined}
              >
                <item.icon className="w-5 h-5 flex-shrink-0" />
                {!collapsed && <span>{item.label}</span>}
              </NavLink>
            );
          })}
        </nav>

        <div className={clsx('p-4 border-t border-gray-700', collapsed && 'hidden')}>
          <div className="text-xs text-gray-500 space-y-1">
            <p>v1.0.0 - Development</p>
            <p className="flex items-center gap-2 text-green-400">
              <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
              Connected
            </p>
          </div>
        </div>
      </div>
    </aside>
  );
}

function Header() {
  return (
    <header className="bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          BackOps Simulator
        </h2>
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 px-3 py-1 rounded-full bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-sm">
            <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse" />
            System Healthy
          </div>
        </div>
      </div>
    </header>
  );
}

function Layout() {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <Sidebar />
      <div className="ml-64 transition-all duration-300 min-h-screen">
        <Header />
        <main className="p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="high-load" element={<HighLoadLab />} />
        <Route path="streaming" element={<HighLoadLab scenario="streaming" />} />
        <Route path="payments" element={<PaymentLab />} />
        <Route path="chaos" element={<FailureInjection />} />
        <Route path="observability" element={<Observability />} />
        <Route path="architecture" element={<Architecture />} />
        <Route path="docs" element={<Documentation />} />
      </Route>
    </Routes>
  );
}