import { Outlet, Link, useLocation, useNavigate } from 'react-router';
import { AppBar, Toolbar, Typography, Container, Box, Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, IconButton, Menu, MenuItem } from '@mui/material';
import {
  Dashboard as DashboardIcon,
  People,
  Inventory,
  ShoppingCart,
  Receipt,
  Factory,
  Description,
  Payment,
  Assignment,
  ListAlt,
  LocalCafe,
  AccountCircle,
  Logout
} from '@mui/icons-material';
import { useAuth } from '@/app/providers/AuthProvider';
import { useState } from 'react';

const drawerWidth = 240;

const menuItems = [
  { path: '/admin', label: 'Dashboard', icon: <DashboardIcon /> },
  { path: '/admin/customers', label: 'Customers', icon: <People /> },
  { path: '/admin/products', label: 'Products', icon: <Inventory /> },
  { path: '/admin/standing-orders', label: 'Standing Orders', icon: <ShoppingCart /> },
  { path: '/admin/orders', label: 'Orders', icon: <Receipt /> },
  { path: '/admin/production', label: 'Production List', icon: <Factory /> },
  { path: '/admin/invoices', label: 'Invoices', icon: <Description /> },
  { path: '/admin/payments', label: 'Payments', icon: <Payment /> },
  { path: '/admin/statements', label: 'Statements', icon: <Assignment /> },
  { path: '/admin/logs', label: 'Logs', icon: <ListAlt /> },
];

export default function AdminLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  const handleMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1, bgcolor: '#1976d2' }}>
        <Toolbar>
          <LocalCafe sx={{ mr: 2 }} />
          <Typography variant="h6" noWrap component="div">
            StoryCoffee - Admin Portal
          </Typography>
          <Box sx={{ flexGrow: 1 }} />
          <IconButton
            size="large"
            onClick={handleMenu}
            color="inherit"
          >
            <AccountCircle />
          </IconButton>
          <Menu
            anchorEl={anchorEl}
            open={Boolean(anchorEl)}
            onClose={handleClose}
          >
            <MenuItem key="name" disabled>
              <Typography variant="body2">{user?.name}</Typography>
            </MenuItem>
            <MenuItem key="email" disabled>
              <Typography variant="caption" color="text.secondary">{user?.email}</Typography>
            </MenuItem>
            <MenuItem key="logout" onClick={handleLogout}>
              <Logout sx={{ mr: 1, fontSize: 20 }} />
              Logout
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>
      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: drawerWidth,
            boxSizing: 'border-box',
          },
        }}
      >
        <Toolbar />
        <Box sx={{ overflow: 'auto' }}>
          <List>
            {menuItems.map((item) => (
              <ListItem key={item.path} disablePadding>
                <ListItemButton
                  component={Link}
                  to={item.path}
                  selected={location.pathname === item.path}
                >
                  <ListItemIcon>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} />
                </ListItemButton>
              </ListItem>
            ))}
          </List>
        </Box>
      </Drawer>
      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        <Container maxWidth="xl">
          <Outlet />
        </Container>
      </Box>
    </Box>
  );
}
