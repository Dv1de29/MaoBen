import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useLocation, useNavigate } from 'react-router-dom';

import './App.css';

//Pages import
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage'
import NavBar from './components/Navbar';
import ProfilePage from './pages/ProfilePage';
import NotFound from './pages/NotFound';
import EditProfilePage from './pages/ProfileEditPage';
import CreatePostPage from './pages/CreatePostPage';
import ChatPage from './pages/ChatPage';
import GroupsPage from './pages/GroupsPage';
import PostPage from './pages/PostPage';

import PostModal from './components/PostModal';
import ProtectedRoute from './components/ProtectedRotes';

import EditPostPage from './pages/EditPost';
import GroupManagePage from './pages/GroupManagePage';


function Layout(){
    const navigate = useNavigate();
    const location = useLocation();
    
    
    const isLogin = location.pathname === '/login' || location.pathname === '/register';
    
    const background = location.state && location.state.background;
    
    const isGuest = sessionStorage.getItem("userRole") === "Guest";

    useEffect(() => {
        if ( !isLogin && !isGuest && !sessionStorage.getItem("userToken") ){
            navigate("/login");
        }
    }, [location.pathname])




    return (
        <>
            <div className="app-layout"> 
                {!isLogin && <NavBar />}

                <main className={`app-main ${isLogin ? "no-nav" : ""}`}>
                    <Routes location={background || location}>
                        <Route path='/' element={<HomePage />} />
                        
                        <Route path='/login' element={<LoginPage />} />
                        <Route path='/register' element={<RegisterPage />} />

                        <Route path='/profile' element={
                            // <ProfilePage />
                            <ProtectedRoute children={< ProfilePage/>}/>
                        } />
                        <Route path='/profile/edit' element={
                            // <EditProfilePage />
                            <ProtectedRoute children={<EditProfilePage />}/>
                        } />
                        <Route path='/profile/:usernamePath' element={<ProfilePage />} />

                        <Route path='/create_post' element={
                            // <CreatePostPage />
                            <ProtectedRoute children={<CreatePostPage />}/>
                        } />

                        <Route path='/direct' element={
                            // <ChatPage />
                            <ProtectedRoute children={<ChatPage />}/>
                        }/>

                        <Route path='/groups' element={
                            <ProtectedRoute children={<GroupsPage />} />}
                        />
                        <Route path='/groups/manage/:id' element={
                            <ProtectedRoute children={<GroupManagePage />} />
                        } />

                        {/* FALLBACK WHEN I REFRESH WITH PAGE */}
                        <Route path='/p/:post_id' element={<PostPage />}/>

                        <Route path='/p/edit/:post_id' element={
                            // <EditPostPage />
                            <ProtectedRoute children={<EditPostPage />}/>
                        } />


                        {/* NOT FOUND ROUTE */}
                        <Route path='*' element={<NotFound />}></Route>
                    </Routes>

                    {background && (
                        <Routes>
                            <Route path='/p/:post_id' element={<PostModal />}></Route>
                        </Routes>
                    )}
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