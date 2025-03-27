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

TestResultSchema.virtual('totalQuestions').get(function () {
    return this.userAnswers.length;
});

TestResultSchema.virtual('correctAnswers').get(function () {
    return this.userAnswers.filter(answer => answer.isCorrect).length;
});

TestResultSchema.virtual('score').get(function () {
    if (this.userAnswers.length === 0) return 0;
    return (this.correctAnswers / this.totalQuestions) * 100;
});

TestResultSchema.set('toJSON', { 
    virtuals: true,
    transform: function(doc, ret) {
        ret.score = parseFloat(ret.score.toFixed(1));
        return ret;
    }
});

TestResultSchema.set('toObject', { virtuals: true });

TestResultSchema.virtual('timedOutQuestions').get(function () {
    return this.userAnswers.filter(answer => answer.timedOut).length;
});

module.exports = mongoose.model('TestResult', TestResultSchema);
