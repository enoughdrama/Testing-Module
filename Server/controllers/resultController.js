const TestResult = require('../models/TestResult');
const Question = require('../models/Question');
const mongoose = require('mongoose');
const User = require('../models/User');
const { xorDecryptFromBase64 } = require('../services/encryption')

// @desc    Submit test result
// @route   POST /api/results
// @access  Private
exports.submitResult = async (req, res, next) => {
    try {
        if (req.body.encrypted === true && req.body.data) {
            console.log('Received encrypted test results');

            const encryptionKey = process.env.ENCRYPTION_KEY;
            const decryptedJson = xorDecryptFromBase64(req.body.data, encryptionKey);

            const decryptedData = JSON.parse(decryptedJson);

            req.body = decryptedData;

            console.log('Decrypted test results successfully');
        }

        req.body.user = req.user.id;

        console.log('Processing test results:', JSON.stringify(req.body, null, 2));

        const allQuestions = await Question.find();
        console.log(`Found ${allQuestions.length} questions in database`);

        allQuestions.forEach(q => console.log(`Question in DB: ${q.text.substring(0, 30)}`));

        const questionMap = {};
        allQuestions.forEach(question => {
            questionMap[question.text] = {
                _id: question._id,
                answers: {}
            };

            if (question.answers && question.answers.length) {
                question.answers.forEach(answer => {
                    questionMap[question.text].answers[answer.text] = answer._id;
                });
            }
        });

        const userAnswers = [];

        for (const answer of req.body.userAnswers) {
            console.log(`Processing answer for question: "${answer.questionText}"`);

            const matchingQuestion = questionMap[answer.questionText];

            if (!matchingQuestion) {
                console.log(`No matching question found for: "${answer.questionText}"`);
                console.log('Available questions:');
                Object.keys(questionMap).forEach(text => {
                    console.log(`- "${text.substring(0, 30)}..."`);
                });
                continue;
            }

            const userAnswer = {
                question: matchingQuestion._id,
                selectedAnswers: [],
                freeTextAnswer: answer.freeTextAnswer || '',
                isCorrect: answer.isCorrect,
                timedOut: answer.timedOut || false
            };

            if (answer.selectedAnswers && answer.selectedAnswers.length > 0) {
                if (answer.selectedAnswerText) {
                    const matchingAnswerId = matchingQuestion.answers[answer.selectedAnswerText];
                    if (matchingAnswerId) {
                        userAnswer.selectedAnswers.push(matchingAnswerId);
                    }

                    if (answer.selectedAnswers.length > 1) {
                        const questionObj = allQuestions.find(q => q._id.equals(matchingQuestion._id));
                        if (questionObj && questionObj.answers) {
                            const correctAnswers = questionObj.answers.filter(a => a.isCorrect);
                            if (correctAnswers.length > 1 && answer.isCorrect) {
                                correctAnswers.forEach(a => {
                                    if (!userAnswer.selectedAnswers.some(id => id.equals(a._id))) {
                                        userAnswer.selectedAnswers.push(a._id);
                                    }
                                });
                            }
                        }
                    }
                }
            }

            userAnswers.push(userAnswer);
        }

        if (userAnswers.length === 0) {
            return res.status(400).json({
                success: false,
                error: 'No valid answers were processed'
            });
        }

        console.log(`Processed ${userAnswers.length} valid answers`);

        const testResult = await TestResult.create({
            user: req.user.id,
            completionTime: req.body.completionTime,
            userAnswers
        });

        console.log('Successfully saved test result with ID:', testResult._id);

        res.status(201).json({
            success: true
        });
    } catch (err) {
        console.error('Error saving test result:', err);
        next(err);
    }
};

exports.getMyResults = async (req, res, next) => {
    try {
        const results = await TestResult.find({ user: req.user.id })
            .populate({
                path: 'userAnswers.question',
                select: 'text type'
            });

        res.status(200).json({
            success: true,
            count: results.length,
            data: results
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Get single result
// @route   GET /api/results/:id
// @access  Private
exports.getResult = async (req, res, next) => {
    try {
        const result = await TestResult.findById(req.params.id)
            .populate({
                path: 'userAnswers.question',
                select: 'text type imageUrl answers'
            })
            .populate({
                path: 'user',
                select: 'fullName'
            });

        if (!result) {
            return res.status(404).json({
                success: false,
                error: 'Result not found'
            });
        }

        if (result.user._id.toString() !== req.user.id && req.user.role !== 'admin') {
            return res.status(401).json({
                success: false,
                error: 'Not authorized to access this result'
            });
        }

        res.status(200).json({
            success: true,
            data: result
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Get all results (admin only)
// @route   GET /api/results/all
// @access  Private (Admin)
exports.getAllResults = async (req, res, next) => {
    try {
        const results = await TestResult.find()
            .populate({
                path: 'user',
                select: 'fullName uniqueKey'
            });

        const resultsWithScores = results.map(result => {
            const totalQuestions = result.userAnswers.length;
            const correctAnswers = result.userAnswers.filter(a => a.isCorrect).length;
            const score = totalQuestions > 0 ? (correctAnswers / totalQuestions) * 100 : 0;

            const resultObj = result.toObject();
            resultObj.score = parseFloat(score.toFixed(1));

            return resultObj;
        });

        res.status(200).json({
            success: true,
            count: results.length,
            data: resultsWithScores
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Delete result
// @route   DELETE /api/results/:id
// @access  Private (Admin)
exports.deleteResult = async (req, res, next) => {
    try {
        const result = await TestResult.findById(req.params.id);

        if (!result) {
            return res.status(404).json({
                success: false,
                error: 'Result not found'
            });
        }

        await result.remove();

        res.status(200).json({
            success: true,
            data: {}
        });
    } catch (err) {
        next(err);
    }
};

// @desc    Get statistics for all results
// @route   GET /api/results/stats
// @access  Private (Admin)
exports.getStats = async (req, res, next) => {
    try {
        const results = await TestResult.find();

        const totalTests = results.length;
        const averageScore = results.reduce((sum, result) => sum + result.score, 0) / totalTests || 0;

        const passingCount = results.filter(result => result.score >= 60).length;
        const passingRate = (passingCount / totalTests) * 100 || 0;

        const timedOutQuestions = results.reduce((sum, result) =>
            sum + result.userAnswers.filter(a => a.timedOut).length, 0);

        const scoreRanges = {
            '90-100': results.filter(r => r.score >= 90 && r.score <= 100).length,
            '80-89': results.filter(r => r.score >= 80 && r.score < 90).length,
            '70-79': results.filter(r => r.score >= 70 && r.score < 80).length,
            '60-69': results.filter(r => r.score >= 60 && r.score < 70).length,
            '0-59': results.filter(r => r.score < 60).length
        };

        res.status(200).json({
            success: true,
            data: {
                totalTests,
                averageScore,
                passingCount,
                passingRate,
                timedOutQuestions,
                scoreRanges
            }
        });
    } catch (err) {
        next(err);
    }
};
