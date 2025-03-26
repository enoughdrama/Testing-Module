function xorEncryptToBase64(input, key) {
    if (!input || !key) {
        throw new Error('Input and key must be provided');
    }

    const inputBytes = Buffer.from(input, 'utf8');
    const keyBytes = Buffer.from(key, 'utf8');
    const result = Buffer.alloc(inputBytes.length);

    // XOR each byte of input with the corresponding byte of the key
    for (let i = 0; i < inputBytes.length; i++) {
        result[i] = inputBytes[i] ^ keyBytes[i % keyBytes.length];
    }

    // Convert the result to base64
    return result.toString('base64');
}

function xorDecryptFromBase64(encodedInput, key) {
    if (!encodedInput || !key) {
        throw new Error('Encoded input and key must be provided');
    }

    // Decode the base64 input
    const encryptedBytes = Buffer.from(encodedInput, 'base64');
    const keyBytes = Buffer.from(key, 'utf8');
    const result = Buffer.alloc(encryptedBytes.length);

    // XOR each byte of encrypted data with the corresponding byte of the key
    for (let i = 0; i < encryptedBytes.length; i++) {
        result[i] = encryptedBytes[i] ^ keyBytes[i % keyBytes.length];
    }

    // Convert the result to a string
    return result.toString('utf8');
}

module.exports = {
    xorEncryptToBase64,
    xorDecryptFromBase64
};