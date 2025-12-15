import { Link } from "react-router-dom";

import { useUser } from "../context/UserContext";

interface UserNav{
    userName: string,
    userImage: string,
}

function NavBar(){
        
        const userContext = useUser().user;
    
        const user: UserNav = {
            userName: userContext?.username || "",
            userImage: userContext?.profilePictureUrl ? userContext.profilePictureUrl : "/assets/img/no_user.png"
        };

        return (
            <nav className="app-nav">
            <ul>
                <li><span>MaoBen</span></li>
                <li><Link to="/">Home</Link></li>
                <li><Link to='/create_post'>Create Post</Link></li>
                <li><Link to="/login" onClick={() => {sessionStorage.clear()}} style={{color: 'red'}}>Log out</Link></li>
                <li className='profile-link'>
                    <span>{user.userName}</span>
                    <Link to='/profile'>
                        <img className='profile-link-image' src={user.userImage ? user.userImage : "/assets/img/no_user.jpg"} alt="" />
                    </Link>
                </li>
            </ul>
        </nav>
    )
}

export default NavBar;