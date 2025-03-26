const express = require('express');
const {
    submitResult,
    getMyResults,
    getResult,
    getAllResults,
    deleteResult,
    getStats
} = require('../controllers/resultController');

const router = express.Router();

const { protect, authorize } = require('../middleware/auth');

// Protect all routes
router.use(protect);

// Admin only routes - MUST be defined BEFORE the /:id route
router.get('/all', authorize('admin'), getAllResults);
router.get('/stats', authorize('admin'), getStats);

// User accessible routes
router.post('/', submitResult);
router.get('/', getMyResults);
router.get('/:id', getResult);
router.delete('/:id', authorize('admin'), deleteResult);

module.exports = router;