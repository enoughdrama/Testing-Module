const express = require('express');
const {
  register,
  login,
  authenticate,
  logout,
  getMe,
  resetAdmin
} = require('../controllers/authController');

const router = express.Router();

const { protect } = require('../middleware/auth');

router.post('/register', register);
router.post('/login', login);
router.post('/authenticate', authenticate);
router.post('/reset-admin', resetAdmin);
router.get('/logout', logout);
router.get('/me', protect, getMe);

module.exports = router;