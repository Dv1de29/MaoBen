import { useState } from "react";

import { Link } from "react-router-dom";

import { useUser } from "../context/UserContext";

import SearchDrawer from "./SearchDrawer";

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faHouse, faMagnifyingGlass, faPlus, faSignOut, faMessage, faUsers, faXmark, faCircleXmark } from '@fortawesome/free-solid-svg-icons';

interface UserNav{
    userName: string,
    userImage: string,
}

function NavBar(){
    const [isSearchOpen, setIsSearchOpen] = useState(false);
        
    const userContext = useUser().user;
    
    const user: UserNav = {
        userName: userContext?.username || "",
        userImage: userContext?.profilePictureUrl ? userContext.profilePictureUrl : "/assets/img/no_user.png"
    };

    const untoggleSearch = () => {
        setIsSearchOpen(false);
    }

    const toggleSearch = () => {
        setIsSearchOpen(!isSearchOpen);
    };

    return (
        <>
        <nav className={`app-nav ${isSearchOpen ? 'nav-narrow' : ''}`}>
            <ul>
                <li className="nav-logo">
                    <span className={`app-name ${isSearchOpen ? 'hidden-text' : ''}`}>MaoBen</span>
                        {/* Optional: Show a small logo icon when narrow if you have one */}
                    {isSearchOpen && <span className="small-logo">MB</span>}
                </li>

                <li>
                <Link to="/" onClick={untoggleSearch}>
                    <FontAwesomeIcon icon={faHouse}/>
                    <span className={isSearchOpen ? 'hidden-text' : ''}>Home</span>
                </Link></li>
                <li>
                <Link to='/create_post' onClick={untoggleSearch}>
                    <FontAwesomeIcon icon={faPlus}/>
                    <span className={isSearchOpen ? 'hidden-text' : ''}>Create Post</span>
                </Link></li>
                <li onClick={toggleSearch}>
                    <div className="search-wrapper noclick-wrapper">
                        <FontAwesomeIcon icon={faMagnifyingGlass}/>
                        <span className={isSearchOpen ? 'hidden-text' : ''}>Search</span>
                    </div>
                </li>

                <li>
                <Link to="/direct" onClick={untoggleSearch}>
                    <FontAwesomeIcon icon={faMessage}/>
                    <span className={isSearchOpen ? 'hidden-text' : ''}>Chat</span>
                </Link></li>

                <li>
                <Link to="/groups" onClick={untoggleSearch}>
                    <FontAwesomeIcon icon={faUsers}/>
                    <span className={isSearchOpen ? 'hidden-text' : ''}>Groups</span>
                </Link></li>

                

                {/* LOGOUT */}
                <li>
                <Link to="/login" onClick={() => {sessionStorage.clear()}} style={{color: 'red'}}>
                    <FontAwesomeIcon icon={faSignOut}/>
                    <span className={isSearchOpen ? 'hidden-text' : ''} style={{ color: 'red'}}>Log out</span>
                </Link></li>


                {/* PROFILE DOWN */}
                <li className='profile-link' onClick={untoggleSearch}>
                    <span className={isSearchOpen ? 'hidden-text' : ''}>{user.userName}</span>
                    <Link to='/profile'>
                        <img className='profile-link-image' src={user.userImage} alt="" />
                    </Link>
                </li>
            </ul>
        </nav>

        {/* 5. The Slide-out Search Drawer */}
        <SearchDrawer isOpen={isSearchOpen} setIsOpen={setIsSearchOpen}/>
        </>
    )
}

export default NavBar;