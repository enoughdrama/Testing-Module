const express = require('express');
const {
    getKeys,
    generateKeys,
    deleteKey
} = require('../controllers/keyController');

const router = express.Router();

const { protect, authorize } = require('../middleware/auth');

// All routes need authentication and admin role
router.use(protect);
router.use(authorize('admin'));

router.route('/')
    .get(getKeys);

router.route('/generate')
    .post(generateKeys);

router.route('/:key')
    .delete(deleteKey);

module.exports = router;