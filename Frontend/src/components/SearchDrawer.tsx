import { useCallback, useEffect, useRef, useState } from 'react';

import '../styles/SearchDrawer.css'

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCircleXmark, faXmark } from '@fortawesome/free-solid-svg-icons';
import type { UsersSearchApiType } from '../assets/types';
import { Link, useNavigate } from 'react-router-dom';

interface SearchDrawerProps {
    isOpen: boolean;
    setIsOpen: (value: boolean) => void,
}

function SearchDrawer({ isOpen, setIsOpen }: SearchDrawerProps) {
    const navigate = useNavigate();

    const [searchValue, setSearchValue] = useState<string>("")
    const [displayUsers, setDisplayUsers] = useState<UsersSearchApiType[]>([])

    const [loading, setLoading] = useState<boolean>(false)

    const AbortControllerRef = useRef<AbortController | null>(null);

    const searchForUsers = useCallback(() => {
        if (AbortControllerRef.current){
            AbortControllerRef.current.abort();
        }

        console.log("Searching for:", searchValue);

        const controller = new AbortController();
        AbortControllerRef.current = controller;

        setLoading(true)

        const fetchUsersSearch = async () => {
            const token = sessionStorage.getItem("userToken")
            const safeSearchValue = encodeURIComponent(searchValue)

            try{
                const res = await fetch(`/api/Profile/allUsers/${safeSearchValue}`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`
                    },
                    signal: controller.signal
                })

                if ( !res.ok ){
                    throw new Error(`Response error:  ${res.status}, ${res.statusText}`)
                }

                const data: UsersSearchApiType[] = await res.json();
                
                setDisplayUsers(data)

            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            } catch (e: any){
                if ( e.name === "AbortError"){
                    console.log("Cancelled previous search")
                    return
                }
                console.error("Error at searching users: ", e);
                setDisplayUsers([])
            } finally{
                if ( !controller.signal.aborted ){
                    setLoading(false)
                }
            }
        }

        fetchUsersSearch()

        // console.log(displayUsers)

    }, [searchValue])

    useEffect(() => {
        if ( !searchValue.trim() ){
            if ( AbortControllerRef.current ) AbortControllerRef.current.abort();
            setDisplayUsers([])
            setLoading(false)
            return
        } 

        const delayDebounceFn = setTimeout(() => {
            searchForUsers();
        }, 800);

        return () => clearTimeout(delayDebounceFn);
        
    }, [searchValue, searchForUsers]);

    // console.log(displayUsers.length, searchValue.trim().length, loading)

    return (
        <div className={`search-drawer ${isOpen ? 'open' : ''}`}>
            <div className="drawer-header">
                <h2>Search</h2>
                <div className="search-input-container">
                    <input
                        type="text" 
                        placeholder="Search..." 
                        value={searchValue}
                        onChange={(e) => {setSearchValue(e.target.value)}}
                    />
                    <button 
                        className="clear-btn" 
                        onClick={() => {setSearchValue("")}}
                    >
                        <FontAwesomeIcon icon={faCircleXmark} />
                    </button>
                </div>
            </div>

            <div className="drawer-content">
                {displayUsers.length > 0 && (
                    <ul className="search-list">
                        {displayUsers.map((user) => (
                            <li 
                                key={user.username} 
                                className="search-item"
                                onClick={() => {
                                    setIsOpen(false)
                                    navigate(`/profile/${user.username}`)
                                }}
                            >
                                {/* Use a user avatar if available, or fallback */}
                                <img 
                                   src={user.profilePictureUrl || "/assets/img/no_user.png"} 
                                   alt={user.username} 
                                />
                                <div className="user-info">
                                    <span className="username">{user.username}</span>
                                    <span className="subtext">{user.name}</span>
                                </div>
                             </li>
                        ))}
                    </ul>
                )}
                {(displayUsers.length === 0 && !loading && searchValue.trim().length !== 0) && (
                    <div className="no-users">
                        <span>NO USERS FOUND</span>
                    </div>
                )}
                {/* <div className="recent-header">
                    <h4>Recent</h4>
                    <button className="clear-all">Clear all</button>
                </div> */}

                {/* Mock Recent Items */}
                {/* <ul className="recent-list">
                    <li className="recent-item">
                        <img src="/assets/img/no_user.png" alt="user" />
                        <div className="user-info">
                            <span className="username">vladdinu1</span>
                            <span className="subtext">Following</span>
                        </div>
                        <button className="remove-recent">
                            <FontAwesomeIcon icon={faXmark} />
                        </button>
                    </li>
                    <li className="recent-item">
                        <img src="/assets/img/no_user.png" alt="user" />
                        <div className="user-info">
                            <span className="username">f1_official</span>
                            <span className="subtext">Formula 1</span>
                        </div>
                        <button className="remove-recent">
                            <FontAwesomeIcon icon={faXmark} />
                        </button>
                    </li>
                </ul> */}
            </div>
        </div>
    );
}

export default SearchDrawer;