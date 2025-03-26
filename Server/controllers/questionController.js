const Question = require('../models/Question');
const { xorEncryptToBase64 } = require('../services/encryption')

exports.getQuestions = async (req, res, next) => {
    try {
        const questions = await Question.find();

        const responseData = {
            success: true,
            count: questions.length,
            data: questions
        };

        const jsonString = JSON.stringify(responseData);
        const encryptionKey = process.env.ENCRYPTION_KEY;

        const encryptedData = xorEncryptToBase64(jsonString, encryptionKey);

        res.status(200).json({
            encrypted: true,
            data: encryptedData
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Get single question
// @route   GET /api/questions/:id
// @access  Private
exports.getQuestion = async (req, res, next) => {
    try {
        const question = await Question.findById(req.params.id);

        if (!question) {
            return res.status(404).json({
                success: false,
                error: 'Question not found'
            });
        }

        res.status(200).json({
            success: true,
            data: question
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Create new question
// @route   POST /api/questions
// @access  Private (Admin only)
exports.createQuestion = async (req, res, next) => {
    try {
        // Validate request body
        const { text, type, imageUrl, answers } = req.body;
        
        if (!text || !type) {
            return res.status(400).json({
                success: false,
                error: 'Please provide question text and type'
            });
        }
        
        // Validate answers if not FreeText
        if (type !== 'FreeText') {
            if (!answers || !Array.isArray(answers) || answers.length === 0) {
                return res.status(400).json({
                    success: false,
                    error: 'Please provide answer options for choice questions'
                });
            }
            
            // Validate that at least one answer is correct
            const hasCorrectAnswer = answers.some(answer => answer.isCorrect);
            if (!hasCorrectAnswer) {
                return res.status(400).json({
                    success: false,
                    error: 'At least one answer must be marked as correct'
                });
            }
        }
        
        const question = await Question.create(req.body);

        res.status(201).json({
            success: true,
            data: question
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Update question
// @route   PUT /api/questions/:id
// @access  Private (Admin only)
exports.updateQuestion = async (req, res, next) => {
    try {
        let question = await Question.findById(req.params.id);

        if (!question) {
            return res.status(404).json({
                success: false,
                error: 'Question not found'
            });
        }
        
        // Validate request body
        const { text, type, answers } = req.body;
        
        if (!text || !type) {
            return res.status(400).json({
                success: false,
                error: 'Please provide question text and type'
            });
        }
        
        // Validate answers if not FreeText
        if (type !== 'FreeText') {
            if (!answers || !Array.isArray(answers) || answers.length === 0) {
                return res.status(400).json({
                    success: false,
                    error: 'Please provide answer options for choice questions'
                });
            }
            
            // Validate that at least one answer is correct
            const hasCorrectAnswer = answers.some(answer => answer.isCorrect);
            if (!hasCorrectAnswer) {
                return res.status(400).json({
                    success: false,
                    error: 'At least one answer must be marked as correct'
                });
            }
        }

        question = await Question.findByIdAndUpdate(req.params.id, req.body, {
            new: true,
            runValidators: true
        });

        res.status(200).json({
            success: true,
            data: question
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Delete question
// @route   DELETE /api/questions/:id
// @access  Private (Admin only)
exports.deleteQuestion = async (req, res, next) => {
    try {
        const question = await Question.findById(req.params.id);

        if (!question) {
            return res.status(404).json({
                success: false,
                error: 'Question not found'
            });
        }

        await question.remove();

        res.status(200).json({
            success: true,
            data: {}
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Evaluate an answer for a question (testing purposes)
// @route   POST /api/questions/:id/evaluate
// @access  Private
exports.evaluateAnswer = async (req, res, next) => {
    try {
        const { selectedAnswerIds, freeTextAnswer } = req.body;
        const question = await Question.findById(req.params.id);

        if (!question) {
            return res.status(404).json({
                success: false,
                error: 'Question not found'
            });
        }

        const isCorrect = question.evaluateAnswer(selectedAnswerIds || [], freeTextAnswer || '');

        res.status(200).json({
            success: true,
            data: {
                isCorrect
            }
        });
    } catch (err) {
        next(err);
    }
};