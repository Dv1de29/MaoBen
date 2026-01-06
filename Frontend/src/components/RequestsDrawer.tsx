import { useCallback, useEffect, useRef, useState } from 'react';



//////// i need to change to also add the buttons for follow
import '../styles/ReqDrawer.css'

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCircleXmark, faXmark } from '@fortawesome/free-solid-svg-icons';
import type { UsersSearchApiType } from '../assets/types';
import { Link, useNavigate } from 'react-router-dom';

interface ReqDrawerProps {
    isOpen: boolean;
    setIsOpen: (value: boolean) => void,
}

function ReqDrawer({ isOpen, setIsOpen }: ReqDrawerProps) {
    const navigate = useNavigate();

    const [displayUsers, setDisplayUsers] = useState<UsersSearchApiType[]>([])

    useEffect(() => {
        const token = sessionStorage.getItem("userToken");

        const fetchReq = async () => {
            try{
                const res = await fetch(`/api/Follow/requests`, {
                    method: 'GET',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    }
                });

                if ( !res.ok ){
                    throw new Error(`Error response: ${res.status}, ${res.statusText}`);
                }


                const data: UsersSearchApiType[] = await res.json();
                
                setDisplayUsers(data)

            } catch(e){
                console.error("Error at getting requests: ", e);
            }
        }

        fetchReq();
    }, []);

    const handleAccept = (username: string) => {
        const token = sessionStorage.getItem("userToken");

        const fetchAcceptFollow = async () => {
            try{
                const res = await fetch(`/api/Follow/accept/${username}`, {
                    method: 'PUT',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    }
                });

                if ( !res.ok ){
                    throw new Error(`Error response: ${res.status}, ${res.statusText}`);
                }

            } catch(e){
                console.error("Error at getting requests: ", e);
            }
        }

        fetchAcceptFollow();
    }

    const handleReject = (username: string) => {
        const token = sessionStorage.getItem("userToken");

        const fetchRejectFollow = async () => {
            try{
                const res = await fetch(`/api/Follow/decline/${username}`, {
                    method: 'DELETE',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    }
                });

                if ( !res.ok ){
                    throw new Error(`Error response: ${res.status}, ${res.statusText}`);
                }

            } catch(e){
                console.error("Error at getting requests: ", e);
            }
        }

        fetchRejectFollow();
    }

    return (
        <div className={`search-drawer ${isOpen ? 'open' : ''}`}>
            <div className="drawer-header">
                <h2>Search</h2>
            </div>

            <div className="drawer-content">
                {displayUsers.length > 0 && (
                    <ul className="search-list">
                        {displayUsers.map((user) => (
                            <li 
                                key={user.username} 
                                className="req-item"
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
                                <button 
                                    className="req-action-btn"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        handleAccept(user.username);
                                    }}
                                >
                                    Accept
                                </button>
                                <button 
                                    className="action-btn btn-reject"
                                    onClick={(e) => {
                                        e.stopPropagation();
                                        handleReject(user.username);
                                    }}
                                >
                                    <FontAwesomeIcon icon={faXmark} />
                                </button>
                             </li>
                        ))}
                    </ul>
                )}
                {(displayUsers.length === 0 ) && (
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

export default ReqDrawer;