import React, { useState } from 'react';
import { type ChangeEvent, type FormEvent } from 'react';

import { Link, useNavigate } from 'react-router-dom';

import UserIcon from '../assets/svg/user-icon.svg'
import OpenLock from '../assets/svg/lock-open-icon.svg'
import CloseLock from '../assets/svg/lock-close-icon.svg'


import '../styles/LoginPage.css';
import { useUser } from '../context/UserContext';

function RegisterPage() {
    const navigate = useNavigate();
    const { refreshUser } = useUser();

    const [formData, setFormData] = useState({
        firstName: 'Ionut',
        lastName: 'CuPulaMica',
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
    });

    const [error, setError] = useState<string>("")

    const [errorMessage, setErrorMessage] = useState({
        emailMessage: "",
        passMessage: "",
        passCheckMessage: "",
    })

    const [showPass, setShowPass] = useState(false)
    const [showPassCheck, setShowPassCheck] = useState(false)

    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prevState => ({
        ...prevState,
        [name]: value
        }));
    };

    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        let ok: boolean = true;
        if ( formData.password.length < 6){
           ok = false;
           setErrorMessage(prev => {
            return {
                ...prev,
                passMessage: "Password must have lenght at least 6",
            }
           })
        }

        if ( formData.password !== formData.confirmPassword ){
            ok = false;
            setErrorMessage(prev => {
            return {
                ...prev,
                passCheckMessage: "Password don't match",
            }
           })
        }

        if ( formData.email.length < 6){
            ok = false;
            setErrorMessage(prev => {
            return {
                ...prev,
                emailMessage: "Email must have length at least 6",
            }
           })
        }

        if ( !ok ){
            return;
        }

        // alert('Login Submitted: Unde e baza de date Ionute');
        const fetchReg = async () => {
            try{
                const res = await fetch("/api/auth/register", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(formData)
                });

                if ( !res.ok ) {
                    const errorData = await res.json();
                    throw new Error(errorData || `Response not ok: ${res.status}, ${res.statusText}`);
                }

                const data = await res.json();

                sessionStorage.setItem("userToken", data.token)
                sessionStorage.setItem("userName", data.username)
                sessionStorage.setItem("userRole", data.role);

                refreshUser();
                navigate("/")
            
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
            } catch(e: any){
                console.error("Register failed:", e);
                setError(e.toString())
            }
        }

        fetchReg();
    };

    return (
        <div className="login-page">
            <div className="login-card">
                <h2>Welcome Back</h2>
                <p className="login-subtitle">Please enter your details to sign in.</p>
                
                <form onSubmit={handleSubmit}>
                    {error && (
                    <div className="error-message" style={{color: 'red', marginBottom: '10px'}}>{error}</div>
                    )}
                    <div className="form-group">
                        <label htmlFor="username">Username</label>
                        <div className="input-container">
                            <input 
                            type="text" 
                            id="username" 
                            name="username" 
                            placeholder="Username"
                            value={formData.username} 
                            onChange={handleChange} 
                            required 
                            />
                            <img src={UserIcon} alt="" />
                        </div>
                        <span id="message-error">{errorMessage.emailMessage}</span>
                    </div>
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
                        <span id="message-error">{errorMessage.emailMessage}</span>
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
                        <span id="message-error">{errorMessage.passMessage}</span>
                    </div>

                    <div className="form-group">
                        <label htmlFor="password-check">Password</label>
                        <div className="input-container">
                            <input 
                            type={showPassCheck ? "text" : "password"} 
                            id="confirmPassword" 
                            name="confirmPassword" 
                            placeholder="••••••••"
                            value={formData.confirmPassword} 
                            onChange={handleChange} 
                            required 
                            />
                            {<img 
                                src={showPassCheck ? OpenLock : CloseLock} alt="" 
                                onClick={() => setShowPassCheck(!showPassCheck)}
                            />}
                        </div>
                        <span id="message-error">{errorMessage.passCheckMessage}</span>
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

export default RegisterPage;