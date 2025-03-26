const User = require('../models/User');

exports.register = async (req, res, next) => {
  try {
    const { fullName, uniqueKey, password, role } = req.body;

    const user = await User.create({
      fullName,
      uniqueKey,
      password,
      role: role || 'user'
    });

    sendTokenResponse(user, 201, res);
  } catch (err) {
    next(err);
  }
};

exports.authenticate = async (req, res, next) => {
  try {
    const { uniqueKey, fullName } = req.body;

    if (!uniqueKey) {
      return res.status(400).json({
        success: false,
        error: 'Please provide a unique key'
      });
    }

    const user = await User.findOne({ uniqueKey }).select('+password');

    if (!user) {
      return res.status(404).json({
        success: false,
        error: 'Invalid unique key'
      });
    }

    if (user.fullName && user.fullName.trim() !== '') {
      return res.status(401).json({
        success: false,
        error: 'This unique key has already been used'
      });
    }

    if (fullName && fullName.trim() !== '') {
      user.fullName = fullName;
      await user.save();
    }

    sendTokenResponse(user, 200, res);
  } catch (err) {
    next(err);
  }
};

exports.login = async (req, res, next) => {
  try {
    const { uniqueKey, password } = req.body;

    if (!uniqueKey || !password) {
      return res.status(400).json({
        success: false,
        error: 'Please provide unique key and password'
      });
    }

    const user = await User.findOne({ uniqueKey }).select('+password');

    if (!user) {
      return res.status(401).json({
        success: false,
        error: 'Invalid credentials'
      });
    }

    const isMatch = await user.matchPassword(password);

    if (!isMatch) {
      return res.status(401).json({
        success: false,
        error: 'Invalid credentials'
      });
    }

    sendTokenResponse(user, 200, res);
  } catch (err) {
    next(err);
  }
};

exports.logout = async (req, res, next) => {
  res.status(200).json({
    success: true,
    data: {}
  });
};

exports.getMe = async (req, res, next) => {
  try {
    const user = await User.findById(req.user.id);

    res.status(200).json({
      success: true,
      data: user
    });
  } catch (err) {
    next(err);
  }
};

exports.resetAdmin = async (req, res, next) => {
  try {
    const { uniqueKey, newPassword } = req.body;
    
    if (!uniqueKey || !newPassword) {
      return res.status(400).json({
        success: false,
        error: 'Please provide uniqueKey and newPassword'
      });
    }
    
    let user = await User.findOne({ uniqueKey });
    
    if (!user) {
      user = new User({
        fullName: 'Administrator',
        uniqueKey,
        password: newPassword,
        role: 'admin'
      });
      
      await user.save();
      
      return res.status(201).json({
        success: true,
        message: `Admin user created with uniqueKey: ${uniqueKey}`
      });
    }
    
    user.role = 'admin';
    user.password = newPassword;
    
    await user.save();
    
    res.status(200).json({
      success: true,
      message: `User ${uniqueKey} password reset and role set to admin`
    });
  } catch (err) {
    next(err);
  }
};

const sendTokenResponse = (user, statusCode, res) => {
  const token = user.getSignedJwtToken();

  const options = {
    expires: new Date(
      Date.now() + process.env.JWT_COOKIE_EXPIRE * 24 * 60 * 60 * 1000
    ),
    httpOnly: true
  };

  if (process.env.NODE_ENV === 'production') {
    options.secure = true;
  }

  res
    .status(statusCode)
    .json({
      success: true,
      token,
      user: {
        id: user._id,
        fullName: user.fullName,
        uniqueKey: user.uniqueKey,
        role: user.role
      }
    });
};