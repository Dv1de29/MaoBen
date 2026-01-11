import { useCallback, useEffect, useRef, useState } from 'react';



//////// i need to change to also add the buttons for follow
import '../styles/ReqDrawer.css'

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCheck, faCircleXmark, faXmark } from '@fortawesome/free-solid-svg-icons';
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

        setDisplayUsers(prev => {
            return prev.filter(u => u.username !== username)
        })
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

        setDisplayUsers(prev => {
            return prev.filter(u => u.username !== username)
        })
    }

    return (
        <div className={`search-drawer ${isOpen ? 'open' : ''}`}>
            <div className="drawer-header">
                <h2>Your requests:</h2>
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


                                <div className="req-actions">
                                    <button 
                                        className="action-btn btn-confirm"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleAccept(user.username);
                                        }}
                                    >
                                        {/* Confirm */}
                                        <FontAwesomeIcon icon={faCheck}/>
                                    </button>
                                    <button 
                                        className="action-btn btn-delete"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            handleReject(user.username);
                                        }}
                                    >
                                        {/* Delete */}
                                        <FontAwesomeIcon icon={faXmark}/>
                                    </button>
                                </div>
                             </li>
                        ))}
                    </ul>
                )}
                {(displayUsers.length === 0 ) && (
                    <div className="no-users">
                        <span>NO USERS FOUND</span>
                    </div>
                )}
            </div>
        </div>
    );
}

export default ReqDrawer;