import React, { useState } from 'react';
import { type ChangeEvent, type FormEvent } from 'react';

import { Link, useNavigate } from 'react-router-dom';

import UserIcon from '../assets/svg/user-icon.svg'
import OpenLock from '../assets/svg/lock-open-icon.svg'
import CloseLock from '../assets/svg/lock-close-icon.svg'


import '../styles/LoginPage.css';

function LoginPage() {
    const navigate = useNavigate();


    const [formData, setFormData] = useState({
        usernameOrEmail: '',
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
        // alert('Login Submitted: Unde e baza de date Ionute');

        const fetchLog = async () => {
            
            try{
                const res = await fetch("http://localhost:5000/api/auth/login", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(formData)
                });

                if ( !res.ok ) {
                    throw new Error(`Response not ok: ${res.status}, ${res.statusText}`)
                }

                const data = await res.json();

                sessionStorage.setItem("userToken", data.token)
                sessionStorage.setItem("userName", data.username)
                sessionStorage.setItem("userRole", data.role);

                navigate("/");

            } catch(e){
                console.log("Login failed:", e)
            }
        }

        fetchLog();
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
                            name="usernameOrEmail" 
                            placeholder="user@example.com"
                            value={formData.usernameOrEmail} 
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