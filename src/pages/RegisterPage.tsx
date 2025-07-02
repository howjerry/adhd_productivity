import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/useAuthStore';
import { Button } from '@/components/ui/Button';
import { Input, InputGroup, Checkbox } from '@/components/ui/Input';
import { Mail, Lock, User, Eye, EyeOff } from 'lucide-react';

export const RegisterPage: React.FC = () => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [agreeToTerms, setAgreeToTerms] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  
  const { register, isLoading, error } = useAuthStore();
  const navigate = useNavigate();

  const handleInputChange = (field: string) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({
      ...prev,
      [field]: e.target.value
    }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => ({
        ...prev,
        [field]: ''
      }));
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.username) {
      newErrors.username = 'Username is required';
    } else if (formData.username.length < 3) {
      newErrors.username = 'Username must be at least 3 characters';
    }

    if (!formData.email) {
      newErrors.email = 'Email is required';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters';
    } else if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(formData.password)) {
      newErrors.password = 'Password must contain uppercase, lowercase, and number';
    }

    if (!formData.confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your password';
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match';
    }

    if (!agreeToTerms) {
      newErrors.terms = 'You must agree to the Terms of Service';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      await register(formData.email, formData.password, formData.username);
      navigate('/dashboard');
    } catch (error) {
      // Error is handled by the auth store
      console.error('Registration failed:', error);
    }
  };

  return (
    <div className="w-full">
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900 mb-2">
          Create your account
        </h2>
        <p className="text-gray-600">
          Join the ADHD productivity community
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <p className="text-red-800 text-sm">{error}</p>
          </div>
        )}

        <InputGroup
          label="Username"
          required
          error={errors.username}
          help="Choose a unique username (3+ characters)"
        >
          <Input
            type="text"
            value={formData.username}
            onChange={handleInputChange('username')}
            placeholder="Enter your username"
            leftIcon={<User className="w-4 h-4" />}
            error={!!errors.username}
            disabled={isLoading}
          />
        </InputGroup>

        <InputGroup
          label="Email address"
          required
          error={errors.email}
        >
          <Input
            type="email"
            value={formData.email}
            onChange={handleInputChange('email')}
            placeholder="Enter your email"
            leftIcon={<Mail className="w-4 h-4" />}
            error={!!errors.email}
            disabled={isLoading}
          />
        </InputGroup>

        <InputGroup
          label="Password"
          required
          error={errors.password}
          help="Minimum 8 characters with uppercase, lowercase, and number"
        >
          <Input
            type={showPassword ? 'text' : 'password'}
            value={formData.password}
            onChange={handleInputChange('password')}
            placeholder="Create a strong password"
            leftIcon={<Lock className="w-4 h-4" />}
            rightIcon={
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="p-1 text-gray-400 hover:text-gray-600"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            }
            error={!!errors.password}
            disabled={isLoading}
          />
        </InputGroup>

        <InputGroup
          label="Confirm Password"
          required
          error={errors.confirmPassword}
        >
          <Input
            type="password"
            value={formData.confirmPassword}
            onChange={handleInputChange('confirmPassword')}
            placeholder="Confirm your password"
            leftIcon={<Lock className="w-4 h-4" />}
            error={!!errors.confirmPassword}
            disabled={isLoading}
          />
        </InputGroup>

        <div className="space-y-4">
          <Checkbox
            id="agree-terms"
            checked={agreeToTerms}
            onChange={setAgreeToTerms}
            label={
              <span className="text-sm text-gray-600">
                I agree to the{' '}
                <a href="#" className="text-indigo-600 hover:text-indigo-500 font-medium">
                  Terms of Service
                </a>{' '}
                and{' '}
                <a href="#" className="text-indigo-600 hover:text-indigo-500 font-medium">
                  Privacy Policy
                </a>
              </span>
            }
          />
          {errors.terms && (
            <p className="text-red-600 text-sm">{errors.terms}</p>
          )}

          <Checkbox
            id="newsletter"
            label={
              <span className="text-sm text-gray-600">
                Send me productivity tips and updates (optional)
              </span>
            }
          />
        </div>

        <Button
          type="submit"
          variant="primary"
          size="lg"
          fullWidth
          loading={isLoading}
          disabled={isLoading}
        >
          Create Account
        </Button>

        <div className="text-center">
          <p className="text-sm text-gray-600">
            Already have an account?{' '}
            <Link
              to="/login"
              className="font-medium text-indigo-600 hover:text-indigo-500"
            >
              Sign in
            </Link>
          </p>
        </div>
      </form>

      {/* ADHD-specific welcome message */}
      <div className="mt-8 p-4 bg-green-50 border border-green-200 rounded-lg">
        <h4 className="text-sm font-medium text-green-900 mb-2">
          🧠 Built for ADHD minds
        </h4>
        <p className="text-xs text-green-700">
          This system is designed specifically for people with ADHD, featuring 
          low-friction task capture, visual timers, energy-based task matching, 
          and gamified progress tracking.
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;