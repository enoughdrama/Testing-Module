const express = require('express');
const {
    getQuestions,
    getQuestion,
    createQuestion,
    updateQuestion,
    deleteQuestion,
    evaluateAnswer
} = require('../controllers/questionController');

const router = express.Router();

const { protect, authorize } = require('../middleware/auth');

// Apply protection to all routes
router.use(protect);

// Routes that everyone can access
router.get('/', getQuestions);
router.get('/:id', getQuestion);
router.post('/:id/evaluate', evaluateAnswer);

// Admin only routes
router.post('/', authorize('admin'), createQuestion);
router.put('/:id', authorize('admin'), updateQuestion);
router.delete('/:id', authorize('admin'), deleteQuestion);

module.exports = router;