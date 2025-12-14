import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';
import './App.css';
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage'
import NavBar from './components/Navbar';
import ProfilePage from './pages/ProfilePage';
import NotFound from './pages/NotFound';
import EditProfilePage from './pages/ProfileEditPage';


function Layout(){
    const navigate = useNavigate();
    const location = useLocation();
    const isLogin = location.pathname === '/login' || location.pathname === '/register';


    useEffect(() => {
        if ( !sessionStorage.getItem("userToken") && !isLogin ){
            navigate("/login");
        }
    }, [location.pathname])



    return (
        <>
            <div className="app-layout"> 
                {!isLogin && <NavBar />}

                <main className="app-main">
                    <Routes>
                        <Route path='/' element={<HomePage />}></Route>
                        <Route path='/login' element={<LoginPage />}></Route>
                        <Route path='/register' element={<RegisterPage />}></Route>

                        <Route path='/profile' element={<ProfilePage />}>
                        </Route>
                        <Route path='/profile/edit' element={<EditProfilePage />} />

                        <Route path='/profile/:usernmae' element={<ProfilePage />}>
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