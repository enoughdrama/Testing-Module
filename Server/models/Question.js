const mongoose = require('mongoose');

const AnswerSchema = new mongoose.Schema({
    text: {
        type: String,
        required: [true, 'Please add answer text']
    },
    isCorrect: {
        type: Boolean,
        required: true,
        default: false
    }
});

const QuestionSchema = new mongoose.Schema({
    text: {
        type: String,
        required: [true, 'Please add question text'],
        trim: true,
        unique: true
    },
    imageUrl: {
        type: String,
        default: ''
    },
    type: {
        type: String,
        enum: ['SingleChoice', 'MultipleChoice', 'FreeText'],
        required: [true, 'Please specify question type']
    },
    answers: [AnswerSchema],
    createdAt: {
        type: Date,
        default: Date.now
    }
});

// Virtual method to get correct answers
QuestionSchema.virtual('correctAnswers').get(function () {
    if (this.type === 'FreeText') return [];
    return this.answers.filter(answer => answer.isCorrect);
});

// Method to evaluate an answer
QuestionSchema.methods.evaluateAnswer = function (selectedAnswerIds, freeTextAnswer) {
    switch (this.type) {
        case 'SingleChoice':
            const correctAnswer = this.answers.find(a => a.isCorrect);
            return selectedAnswerIds.length === 1 &&
                selectedAnswerIds[0].toString() === correctAnswer._id.toString();

        case 'MultipleChoice':
            const correctAnswerIds = this.answers
                .filter(a => a.isCorrect)
                .map(a => a._id.toString());

            return selectedAnswerIds.length === correctAnswerIds.length &&
                selectedAnswerIds.every(id => correctAnswerIds.includes(id.toString()));

        case 'FreeText':
            return !!(freeTextAnswer && freeTextAnswer.trim());

        default:
            return false;
    }
};

// Add text search index
QuestionSchema.index({ text: 'text' });

module.exports = mongoose.model('Question', QuestionSchema);