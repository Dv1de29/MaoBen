import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom';
import './App.css';
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage'
import NavBar from './components/Navbar';
import ProfilePage from './pages/ProfilePage';
import NotFound from './pages/NotFound';
import EditProfilePage from './pages/ProfileEditPage';


function Layout(){
    const location = useLocation();
    const isLogin = location.pathname === '/login' || location.pathname === '/register';

    return (
        <>
            <div className="app-layout"> 
                {!isLogin && <NavBar />}

                <main className="app-main">
                    <Routes>
                        <Route path='/' element={<HomePage />}></Route>
                        <Route path='/login' element={<LoginPage />}></Route>
                        <Route path='/register' element={<RegisterPage />}></Route>
                        {/* Matches /profile (e.g. current user) */}
                        <Route path='/profile' element={<ProfilePage />}>
                            <Route path='edit' element={<EditProfilePage />} />
                        </Route>

                        {/* Matches /profile/123 (specific user) */}
                        <Route path='/profile/:id' element={<ProfilePage />}>
                            <Route path='edit' element={<EditProfilePage />} />
                        </Route>
                        <Route path='*' element={<NotFound />}></Route>
                    </Routes>
                </main>
            </div>
        </>
    )
}

function App() {


    return (
        <Router>
            <Layout />
        </Router>
    );
}

export default App;