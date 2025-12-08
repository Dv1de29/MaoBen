import React, { useState } from 'react';
import { type ChangeEvent, type FormEvent } from 'react';

import { Link } from 'react-router-dom';

import UserIcon from '../assets/svg/user-icon.svg'
import OpenLock from '../assets/svg/lock-open-icon.svg'
import CloseLock from '../assets/svg/lock-close-icon.svg'


import '../styles/LoginPage.css';

function LoginPage() {
    const [formData, setFormData] = useState({
        email: '',
        password: ''
    });

    const [showPass, setShowPass] = useState(false)

    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prevState => ({
        ...prevState,
        [name]: value
        }));
    };

    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        alert('Login Submitted: Unde e baza de date Ionute');
    };

    return (
        <div className="login-page">
            <div className="login-card">
                <h2>Welcome Back</h2>
                <p className="login-subtitle">Please enter your details to sign in.</p>
                
                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label htmlFor="email">Email Address</label>
                        <div className="input-container">
                            <input 
                            type="email" 
                            id="email" 
                            name="email" 
                            placeholder="user@example.com"
                            value={formData.email} 
                            onChange={handleChange} 
                            required 
                            />
                            <img src={UserIcon} alt="" />
                        </div>
                    </div>

                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <div className="input-container">
                            <input 
                            type={showPass ? "text" : "password"} 
                            id="password" 
                            name="password" 
                            placeholder="••••••••"
                            value={formData.password} 
                            onChange={handleChange} 
                            required 
                            />
                            {<img 
                                src={showPass ? OpenLock : CloseLock} alt="" 
                                onClick={() => setShowPass(!showPass)}
                            />}
                        </div>
                    </div>

                    <button type="submit" className="login-btn">Sign In</button>
                </form>

                <div className="login-footer">
                    <p>Don't have an account? <Link to="/register">Sign up</Link></p>
                </div>
            </div>
            
            <footer className="app-footer">
                <p>© 2025 si Ionut inca o suge</p>
            </footer>
        </div>
    );
}

export default LoginPage;