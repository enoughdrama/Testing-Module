const User = require('../models/User');
const bcrypt = require('bcryptjs');
const crypto = require('crypto');

// @desc    Get all keys
// @route   GET /api/keys
// @access  Private (Admin only)
exports.getKeys = async (req, res, next) => {
    try {
        // Find all users with uniqueKey (which are essentially our keys)
        const users = await User.find({}, 'uniqueKey fullName createdAt');

        // Format the response
        const keys = users.map(user => ({
            uniqueKey: user.uniqueKey,
            isUsed: user.fullName && user.fullName.trim() !== '',
            usedBy: user.fullName && user.fullName.trim() !== '' ? user.fullName : null,
            createdAt: user.createdAt
        }));

        res.status(200).json({
            success: true,
            count: keys.length,
            data: keys
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Generate unique keys
// @route   POST /api/keys/generate
// @access  Private (Admin only)
exports.generateKeys = async (req, res, next) => {
    try {
        const { count = 1, prefix = '' } = req.body;

        // Validate count
        const keyCount = parseInt(count, 10);
        if (isNaN(keyCount) || keyCount < 1 || keyCount > 100) {
            return res.status(400).json({
                success: false,
                error: 'Invalid count. Must be between 1 and 100.'
            });
        }

        // Generate specified number of unique keys
        const generatedKeys = [];

        for (let i = 0; i < keyCount; i++) {
            // Generate a random key
            let uniqueKey;
            let isUnique = false;

            // Keep trying until we get a unique key
            while (!isUnique) {
                // Generate a random key of 6 characters
                const randomKey = crypto.randomBytes(3).toString('hex').toUpperCase();
                uniqueKey = prefix + randomKey;

                // Check if key already exists
                const existingUser = await User.findOne({ uniqueKey });
                if (!existingUser) {
                    isUnique = true;
                }
            }

            // Create a new user with the generated key
            // Set default password same as uniqueKey for simplicity
            const salt = await bcrypt.genSalt(10);
            const hashedPassword = await bcrypt.hash(uniqueKey, salt);

            await User.create({
                uniqueKey,
                fullName: '', // Empty name means unused key
                password: hashedPassword,
                role: 'user'
            });

            generatedKeys.push(uniqueKey);
        }

        res.status(201).json({
            success: true,
            count: generatedKeys.length,
            data: generatedKeys
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Delete a key
// @route   DELETE /api/keys/:key
// @access  Private (Admin only)
exports.deleteKey = async (req, res, next) => {
    try {
        const uniqueKey = req.params.key;

        // Find the user with this key
        const user = await User.findOne({ uniqueKey });

        if (!user) {
            return res.status(404).json({
                success: false,
                error: 'Key not found'
            });
        }

        // Check if key is already used
        if (user.fullName && user.fullName.trim() !== '') {
            return res.status(400).json({
                success: false,
                error: 'Cannot delete a key that has already been used'
            });
        }

        // Delete the user/key
        await user.remove();

        res.status(200).json({
            success: true,
            data: {}
        });
    } catch (err) {
        next(err);
    }
};