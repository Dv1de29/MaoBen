import React, { useState } from 'react';
import { type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';

// Icons
import UserIcon from '../assets/svg/user-icon.svg';
import OpenLock from '../assets/svg/lock-open-icon.svg';
import CloseLock from '../assets/svg/lock-close-icon.svg';
import { faEnvelope } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

import '../styles/LoginPage.css';
import { useUser } from '../context/UserContext';

function RegisterPage() {
    const navigate = useNavigate();
    const { refreshUser } = useUser();

    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        username: '',
        email: '',
        password: '',
        confirmPassword: '',
    });

    const [error, setError] = useState<string>("");
    
    const [errorMessage, setErrorMessage] = useState({
        firstNameMessage: "",
        lastNameMessage: "",
        usernameMessage: "",
        emailMessage: "",
        passMessage: "",
        passCheckMessage: "",
    });

    const [showPass, setShowPass] = useState(false);
    const [showPassCheck, setShowPassCheck] = useState(false);

    const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData(prevState => ({ ...prevState, [name]: value }));
        
        // Resetăm eroarea câmpului pe măsură ce utilizatorul scrie
        const fieldKey = name === "confirmPassword" ? "passCheckMessage" : 
                         name === "password" ? "passMessage" : 
                         `${name}Message`;
                         
        setErrorMessage(prev => ({ ...prev, [fieldKey]: "" }));
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        
        // Resetăm toate erorile înainte de a trimite cererea la server
        setError("");
        setErrorMessage({
            firstNameMessage: "", lastNameMessage: "", usernameMessage: "",
            emailMessage: "", passMessage: "", passCheckMessage: "",
        });

        // VALIDĂRI FRONTEND ELIMINATE: Lăsăm totul pe seama DTO-ului și Identity de pe Backend

        try {
            const res = await fetch("/api/auth/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(formData)
            });

            const data = await res.json();

            if (!res.ok) {
                // A) Tratăm DTO DataAnnotations (Erorile din ModelState)
                // Format: { errors: { "Username": ["..."], "Password": ["..."] } }
                if (data.errors) {
                    Object.keys(data.errors).forEach((key) => {
                        const messages = data.errors[key].join(" ");
                        
                        // Mapare nume Backend -> nume State Frontend
                        let fieldName = `${key.charAt(0).toLowerCase() + key.slice(1)}Message`;
                        
                        // Corecție pentru câmpurile cu nume diferite în State
                        if (key === "Password") fieldName = "passMessage";
                        if (key === "ConfirmPassword") fieldName = "passCheckMessage";

                        setErrorMessage(prev => ({ ...prev, [fieldName]: messages }));
                    });
                }
                // B) Tratăm erorile din ASP.NET Identity (Array de obiecte)
                else if (Array.isArray(data)) {
                    data.forEach((err: any) => {
                        const code = err.code.toLowerCase();
                        const desc = err.description;
                        if (code.includes("password")) setErrorMessage(prev => ({ ...prev, passMessage: desc }));
                        else if (code.includes("email")) setErrorMessage(prev => ({ ...prev, emailMessage: desc }));
                        else if (code.includes("user")) setErrorMessage(prev => ({ ...prev, usernameMessage: desc }));
                    });
                } 
                // C) Tratăm mesajele manuale din Controller (BadRequest(new { message = "..." }))
                else if (data.message) {
                    const msg = data.message.toLowerCase();
                    if (msg.includes("email")) setErrorMessage(prev => ({ ...prev, emailMessage: data.message }));
                    else if (msg.includes("username")) setErrorMessage(prev => ({ ...prev, usernameMessage: data.message }));
                    else if (msg.includes("first name")) setErrorMessage(prev => ({ ...prev, firstNameMessage: data.message }));
                    else if (msg.includes("last name")) setErrorMessage(prev => ({ ...prev, lastNameMessage: data.message }));
                    else setError(data.message);
                }
                return;
            }

            // SUCCES - Utilizatorul a trecut de validările de pe server
            sessionStorage.setItem("userToken", data.token);
            sessionStorage.setItem("userName", data.username);
            sessionStorage.setItem("userRole", data.role);
            refreshUser();
            navigate("/");

        } catch (e: any) {
            setError("Server connection failed. Please try again later.");
        }
    };

    return (
        <div className="login-page">
            <div className="login-card">
                <h2 className="login-title">Please enter your details to sign up</h2>
                
                <form onSubmit={handleSubmit} noValidate>
                    <div id='name-group'>
                        <div className="form-group">
                            <label htmlFor="firstName">First Name</label>
                            <div className="input-container">
                                <input 
                                    type="text" id="firstName" name="firstName" placeholder="First Name"
                                    value={formData.firstName} onChange={handleChange} 
                                    className={errorMessage.firstNameMessage ? "input-error" : ""} required 
                                />
                            </div>
                            {errorMessage.firstNameMessage && <span className="field-error">{errorMessage.firstNameMessage}</span>}
                        </div>
                        <div className="form-group">
                            <label htmlFor="lastName">Last Name</label>
                            <div className="input-container">
                                <input 
                                    type="text" id="lastName" name="lastName" placeholder="Last Name"
                                    value={formData.lastName} onChange={handleChange} 
                                    className={errorMessage.lastNameMessage ? "input-error" : ""} required 
                                />
                            </div>
                            {errorMessage.lastNameMessage && <span className="field-error">{errorMessage.lastNameMessage}</span>}
                        </div>
                    </div>

                    <div className="form-group">
                        <label htmlFor="username">Username</label>
                        <div className="input-container">
                            <input 
                                type="text" id="username" name="username" placeholder="Username"
                                value={formData.username} onChange={handleChange} 
                                className={errorMessage.usernameMessage ? "input-error" : ""} required 
                            />
                            <img src={UserIcon} alt="user" />
                        </div>
                        {errorMessage.usernameMessage && <span className="field-error">{errorMessage.usernameMessage}</span>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="email">Email Address</label>
                        <div className="input-container">
                            <input 
                                type="email" id="email" name="email" placeholder="user@example.com"
                                value={formData.email} onChange={handleChange} 
                                className={errorMessage.emailMessage ? "input-error" : ""} required 
                            />
                            <FontAwesomeIcon icon={faEnvelope} className="input-icon-fa" />
                        </div>
                        {errorMessage.emailMessage && <span className="field-error">{errorMessage.emailMessage}</span>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <div className="input-container">
                            <input 
                                type={showPass ? "text" : "password"} id="password" name="password" placeholder="••••••••"
                                value={formData.password} onChange={handleChange} 
                                className={errorMessage.passMessage ? "input-error" : ""} required 
                            />
                            <img 
                                src={showPass ? OpenLock : CloseLock} alt="toggle" 
                                onClick={() => setShowPass(!showPass)} className="toggle-password" 
                            />
                        </div>
                        {errorMessage.passMessage && <span className="field-error">{errorMessage.passMessage}</span>}
                    </div>

                    <div className="form-group">
                        <label htmlFor="confirmPassword">Confirm Password</label>
                        <div className="input-container">
                            <input 
                                type={showPassCheck ? "text" : "password"} id="confirmPassword" name="confirmPassword" placeholder="••••••••"
                                value={formData.confirmPassword} onChange={handleChange} 
                                className={errorMessage.passCheckMessage ? "input-error" : ""} required 
                            />
                            <img 
                                src={showPassCheck ? OpenLock : CloseLock} alt="toggle" 
                                onClick={() => setShowPassCheck(!showPassCheck)} className="toggle-password" 
                            />
                        </div>
                        {errorMessage.passCheckMessage && <span className="field-error">{errorMessage.passCheckMessage}</span>}
                    </div>

                    {error && <div className="error-message general-error">{error}</div>}

                    <button type="submit" className="login-btn">Register</button>
                </form>

                <div className="login-divider"><span>or</span></div>

                <div className="secondary-actions">
                    <Link to="/login" className="btn-outline">Sign In</Link>
                    <button type="button" onClick={() => { sessionStorage.setItem("userRole", "Guest"); refreshUser(); navigate("/"); }} className="btn-ghost">Try as Guest</button>
                </div>
            </div>

            <footer className="app-footer">
                <p>© 2026 si Ionut inca nu stie fotbal</p>
            </footer>
        </div>
    );
}

export default RegisterPage;