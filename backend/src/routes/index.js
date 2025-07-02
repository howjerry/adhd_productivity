const express = require('express');
const router = express.Router();

// Import route modules
// const authRoutes = require('./auth');
// const userRoutes = require('./users');
// const taskRoutes = require('./tasks');
// const analyticsRoutes = require('./analytics');

// API status endpoint
router.get('/', (req, res) => {
  res.json({
    success: true,
    message: 'ADHD Productivity API v1.0.0',
    timestamp: new Date().toISOString(),
    endpoints: {
      auth: '/auth',
      users: '/users',
      tasks: '/tasks',
      analytics: '/analytics'
    }
  });
});

// Route handlers
// router.use('/auth', authRoutes);
// router.use('/users', userRoutes);
// router.use('/tasks', taskRoutes);
// router.use('/analytics', analyticsRoutes);

module.exports = router;