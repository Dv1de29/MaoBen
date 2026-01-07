import { useState } from "react";
import { Link, useLocation } from "react-router-dom"; // Added useLocation
import { useUser } from "../context/UserContext";
import SearchDrawer from "./SearchDrawer";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faHouse, faMagnifyingGlass, faPlus, faSignOut, faMessage, faUsers, faBars } from '@fortawesome/free-solid-svg-icons';
import ReqDrawer from "./RequestsDrawer";

interface UserNav {
    userName: string,
    userImage: string,
    handle: string // Added mock handle for design accuracy
}

function NavBar() {
    const [isSearchOpen, setIsSearchOpen] = useState(false);
    const [isReqOpen, setisReqOpen] = useState<boolean>(false);

    const isDrawerOpen = isSearchOpen || isReqOpen;

    const location = useLocation(); // Hook to get current route
    const userContext = useUser().user;

    const user: UserNav = {
        userName: userContext?.username || "Guest",
        handle: userContext?.username ? `@${userContext.username.toLowerCase()}` : "@guest",
        userImage: userContext?.profilePictureUrl ? userContext.profilePictureUrl : "/assets/img/no_user.png"
    };

    const untoggleSearch = () => setIsSearchOpen(false);
    const untoggleReq = () => setisReqOpen(false);
    const untoggleAll  = () => {
        untoggleReq();
        untoggleSearch();
    }
    
    const toggleSearch = () => setIsSearchOpen(!isSearchOpen);
    const toggleReq = () => setisReqOpen(!isReqOpen);

    // Helper to check active state
    const isActive = (path: string) => location.pathname === path ? 'active' : '';

    return (
        <>
            <nav className={`app-nav ${isDrawerOpen ? 'nav-narrow' : ''}`}>
                
                {/* 1. Header / Logo Area */}
                <div className="nav-header">
                    <div className="logo-icon-bg">
                        <FontAwesomeIcon icon={faBars} />
                    </div>
                    <span className={`app-name ${isDrawerOpen ? 'hidden-text' : ''}`}>MaoBen</span>
                </div>

                {/* 2. Main Navigation Links */}
                <ul className="nav-links">
                    <li>
                        <Link to="/" onClick={untoggleAll} className={`nav-item ${isActive('/')}`}>
                            <FontAwesomeIcon icon={faHouse} />
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Home</span>
                        </Link>
                    </li>
                    
                    <li>
                        <Link to="/profile" onClick={untoggleAll} className={`nav-item ${isActive('/profile')}`}>
                            <img src={user.userImage} alt="profile" className="nav-mini-avatar"/>
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Profile</span>
                        </Link>
                    </li>

                    <li onClick={() => {
                            untoggleReq();
                            toggleSearch();
                        }}>
                        <div className={`nav-item ${isSearchOpen ? 'active' : ''} noclick-wrapper`}>
                            <FontAwesomeIcon icon={faMagnifyingGlass} />
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Search</span>
                        </div>
                    </li>

                    <li onClick={() => {
                            untoggleSearch();
                            toggleReq();
                        }}>
                        <div className={`nav-item ${isReqOpen ? 'active' : ''} noclick-wrapper`}>
                            <FontAwesomeIcon icon={faBars} />
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Requests</span>
                        </div>
                    </li>

                    <li>
                        <Link to="/direct" onClick={untoggleAll} className={`nav-item ${isActive('/direct')}`}>
                            <FontAwesomeIcon icon={faMessage} />
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Messages</span>
                        </Link>
                    </li>

                    <li>
                        <Link to="/groups" onClick={untoggleAll} className={`nav-item ${isActive('/groups')}`}>
                            <FontAwesomeIcon icon={faUsers} />
                            <span className={isDrawerOpen ? 'hidden-text' : ''}>Groups</span>
                        </Link>
                    </li>
                </ul>

                {/* 3. "Create Post" - Distinct CTA Button */}
                <div className="nav-cta-container">
                    <Link to='/create_post' onClick={untoggleAll} className="create-post-btn">
                        <FontAwesomeIcon icon={faPlus} />
                        <span className={isDrawerOpen ? 'hidden-text' : ''}>New Post</span>
                    </Link>
                </div>

                {/* 4. Bottom Profile Card */}
                <div className="nav-footer">
                    <Link to="/profile" className="user-card"
                        onClick={untoggleAll}
                    >
                        <img className='user-card-img' src={user.userImage} alt="" />
                        <div className={`user-card-info ${isDrawerOpen ? 'hidden-text' : ''}`}>
                            <span className="user-name">{user.userName}</span>
                            <span className="user-handle">{user.handle}</span>
                        </div>
                    </Link>
                    
                    {/* Logout Mini Button */}
                    <Link to="/login" onClick={() => {sessionStorage.clear()}} className={`logout-btn ${isDrawerOpen ? 'hidden-text' : ''}`}>
                        <FontAwesomeIcon icon={faSignOut} />
                    </Link>
                </div>
            </nav>

            {isSearchOpen && (<SearchDrawer isOpen={isSearchOpen} setIsOpen={setIsSearchOpen} />)}
            {isReqOpen && (<ReqDrawer isOpen={isReqOpen} setIsOpen={setisReqOpen} />)}
        </>
    )
}

export default NavBar;