const mongoose = require('mongoose');

const UserAnswerSchema = new mongoose.Schema({
    question: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'Question',
        required: true
    },
    selectedAnswers: [{
        type: mongoose.Schema.Types.ObjectId,
        ref: 'Question.answers'
    }],
    freeTextAnswer: {
        type: String,
        default: ''
    },
    isCorrect: {
        type: Boolean,
        required: true
    },
    timedOut: {
        type: Boolean,
        default: false
    }
});

const TestResultSchema = new mongoose.Schema({
    user: {
        type: mongoose.Schema.Types.ObjectId,
        ref: 'User',
        required: true
    },
    completionTime: {
        type: Date,
        default: Date.now
    },
    userAnswers: [UserAnswerSchema],
    createdAt: {
        type: Date,
        default: Date.now
    }
});

// Virtual property to calculate total questions
TestResultSchema.virtual('totalQuestions').get(function () {
    return this.userAnswers.length;
});

// Virtual property to calculate correct answers
TestResultSchema.virtual('correctAnswers').get(function () {
    return this.userAnswers.filter(answer => answer.isCorrect).length;
});

// Virtual property to calculate score
TestResultSchema.virtual('score').get(function () {
    if (this.userAnswers.length === 0) return 0;
    return (this.correctAnswers / this.totalQuestions) * 100;
});

// Make virtuals available in JSON responses
TestResultSchema.set('toJSON', { 
    virtuals: true,
    transform: function(doc, ret) {
        // Make sure score is a number, not a Decimal128
        ret.score = parseFloat(ret.score.toFixed(1));
        return ret;
    }
});

// Make virtuals available when converting to objects
TestResultSchema.set('toObject', { virtuals: true });

// Virtual property to calculate timed out questions
TestResultSchema.virtual('timedOutQuestions').get(function () {
    return this.userAnswers.filter(answer => answer.timedOut).length;
});

module.exports = mongoose.model('TestResult', TestResultSchema);