import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import type { UserProfileApiType } from "../assets/types";
import { useUser } from "../context/UserContext";

interface UserNav{
    userName: string,
    userImage: string,
}

// const INITIAL_USER: UserNav = {
//     userName: "",
//     userImage: "",
// }


// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// WHEN THE USER IS CHANGED OUTSIDE THE NAVBAR, THE NAVBAR DOESNT REFETCH
// I CAN REFRESH THE PAGE BY CODE, BUT IT"S UGLY

/// A GOOD IMPLEMENTATION WOULD BE A GLOBAL STATE INSIDE A PROVIDER THAT HOLDS THE USER

function NavBar(){

    /// this is how i simulate a user existing in localstorage
    // sessionStorage.setItem("userImagePath", "/assets/img/download.jpg")
    // localStorage.setItem("userName", "Mr_Orange");
    
    // const [user, setUser] = useState<UserNav>(INITIAL_USER) 
    
    // useEffect(() => {
        //     const fetchUser = async () => {
            //         const token = sessionStorage.getItem("userToken")
            
            //         try{
                //             const res = await fetch("/api/Profile", {
                    //                 method: "GET",
                    //                 headers: {
                        //                     'Authorization': `Bearer ${token}`,
                        //                     // 'Content-Type': 'application/json'
                        //                 }
                        //             })
                        
                        //             if ( !res.ok ){
                            //                 throw new Error(`Response error: ${res.status}, ${res.statusText}`)
                            //             }
                            
                            //             const data: UserProfileApiType = await res.json();
                            
                            //             setUser({
                                //                 userName: data.username,
    //                 userImage: data.profilePictureUrl || "/assets/img/no_user.png",
    //             })
    //         }catch(e){
        //             console.error("Error at fetching: ", e);
        //         }
        //     }
        
        //     fetchUser();
        // }, [])
        
        
        const userContext = useUser().user;
    
        const user: UserNav = {
            userName: userContext?.username || "",
            userImage: userContext?.profilePictureUrl || "/assets/img/no_user.png"
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