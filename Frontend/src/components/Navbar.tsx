import { Link } from "react-router-dom";


function NavBar(){

    /// this is how i simulate a user existing in localstorage
    // sessionStorage.setItem("userImagePath", "/assets/img/download.jpg")
    // localStorage.setItem("userName", "Mr_Orange");

    const userImage: string | null = sessionStorage.getItem("userImagePath") || "/assets/img/no_user.png";
    console.log(userImage);
    const userName: string | null = sessionStorage.getItem("userName") || "Guest";

    console.log(userImage)

    return (
        <nav className="app-nav">
            <ul>
                <li><span>MaoBen</span></li>
                <li><Link to="/">Home</Link></li>
                <li><Link to="/login" style={{color: 'red'}}>Login</Link></li>
                <li className='profile-link'>
                    <span>{userName}</span>
                    <Link to='/profile'>
                        <img className='profile-link-image' src={userImage ? userImage : ""} alt="" />
                    </Link>
                </li>
            </ul>
        </nav>
    )
}

export default NavBar;