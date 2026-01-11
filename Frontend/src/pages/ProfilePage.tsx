import '../styles/ProfilePage.css'; 

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCog, faArrowLeft, faGlobe, faPaperPlane, faTimes } from '@fortawesome/free-solid-svg-icons';

import type { PostType, PostApiType, UserProfileType } from '../assets/types';

import { Link, useNavigate, useParams, useLocation } from 'react-router-dom';
import { useCallback, useEffect, useState } from 'react';
import { useUser } from '../context/UserContext';

const ProfilePage = () => {
    const navigate = useNavigate();
    const location = useLocation(); // Needed for state background logic

    const { usernamePath } = useParams();
    const { user: contextUser } = useUser();

    const isMyProfile = !usernamePath || usernamePath === contextUser.username;
    const isGuest = (sessionStorage.getItem("userRole") === "Guest");

    const [displayUser, setDisplayUser] = useState<UserProfileType | null>(
        isMyProfile ? contextUser : null
    );
    
    const [posts, setPosts] = useState<PostType[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [doIFollow, setDoIFollow] = useState<"Accepted" | "None" | "Pending">("None");

    // --- NEW STATE FOR MESSAGE MODAL ---
    const [isMessageModalOpen, setIsMessageModalOpen] = useState(false);
    const [messageText, setMessageText] = useState("");
    const [sendingMessage, setSendingMessage] = useState(false);

    const userPrivacy = (displayUser?.privacy === true) && 
                        !isMyProfile &&               
                        doIFollow !== "Accepted";

    // ... [EXISTING USE EFFECTS FOR FETCHING USER AND POSTS - KEEP AS IS] ...
    useEffect(() => {
        if (isMyProfile) {
            setDisplayUser(contextUser); 
        } else {
            setDisplayUser(null); 
        }

        const loadData = async () => {
            setLoading(true);
            const token = sessionStorage.getItem("userToken");
            try{
                ///fetching user
                let currentUserData = isMyProfile ? contextUser : null;
                if (!isMyProfile) {
                    const res = await fetch(`/api/Profile/${usernamePath}`, {
                        method: "GET",
                        headers: {
                            "Authorization": `Bearer ${token}`,
                            "Content-Type": "application/json",
                        }
                    });
                    if (!res.ok) throw new Error(`User not found`);
                    currentUserData = await res.json();
                    setDisplayUser(currentUserData);
                }

                if (!currentUserData) return;

                /// fetching posts
                let postsRes: Response;
                if (isMyProfile) {
                    postsRes = await fetch(`/api/Posts/my_posts`, {
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                } else {
                    postsRes = await fetch(`/api/Posts/ByOwner/${usernamePath}`, {
                        headers: { 'Authorization': `Bearer ${token}` }
                    });
                }

                if (postsRes.ok) {
                    const data = await postsRes.json();
                    console.log(data);
                    const transformedPosts = data
                        .filter((postData: PostApiType) => postData.image_path) // 1. Filter out null/empty strings
                        .map((postData: PostApiType) => ({                      // 2. Transform the remaining posts
                            id: postData.id,
                            owner: postData.owner,
                            img_path: postData.image_path,
                            nr_likes: postData.nr_likes,
                            nr_comm: postData.nr_comms,
                            has_liked: false,
                        }));
                    setPosts(transformedPosts);
                }
            } catch (e) {
                console.error("Error loading profile:", e);
            } finally {
                setLoading(false);
            }
        };
        loadData();
    }, [usernamePath, contextUser, isMyProfile]); // Removed doIFollow to prevent infinite loop if following logic changes user count

    // ... [EXISTING USE EFFECT FOR FOLLOW STATUS - KEEP AS IS] ...
    useEffect(() => {
        if ( isGuest || isMyProfile ) return;
        const setFollow = async () => {
            const token = sessionStorage.getItem("userToken")
            try{
                const res = await fetch(`/api/Follow/status/${usernamePath}`, {
                    headers: { 'Authorization': `Bearer ${token}` }
                })
                if ( !res.ok) throw new Error(`Response error`)
                const data = await res.json();
                setDoIFollow(data.status);
            } catch (e){ console.error(e) }
        }
        setFollow()
    }, [isMyProfile, isGuest, usernamePath]);


    const handleFollow = useCallback(() => {
        if ( isGuest ) return;
        const follow = async () => {
            if ( isMyProfile ) return;
            const token = sessionStorage.getItem("userToken");
            try{
                const res = await fetch(`/api/Follow/${doIFollow !== "None" ? "unfollow/" : ""}${usernamePath}`, {
                    method: `${doIFollow !== "None" ? "DELETE" : "POST"}`,
                    headers: {
                        "Authorization": `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                });
                if ( !res.ok ) throw new Error(`${res.status}`)
                setDoIFollow(doIFollow === "None" ? `${!displayUser?.privacy ? "Accepted" : "Pending"}` : "None")
            } catch(e){ console.error("Follow error: ", e); }
        }
        follow();
    }, [doIFollow, usernamePath, displayUser, isGuest, isMyProfile]);


    // --- NEW MESSAGE HANDLER ---
    const handleSendMessage = async () => {
        if (!messageText.trim()) return;
        
        setSendingMessage(true);
        const token = sessionStorage.getItem("userToken");

        try {
            const res = await fetch(`/api/DirectMessages/send/${usernamePath}`, {
                method: "POST",
                headers: {
                    "Authorization": `Bearer ${token}`,
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ content: messageText }) // Assuming API expects { content: "string" }
            });

            if (res.ok) {
                alert("Message sent!");
                setIsMessageModalOpen(false);
                setMessageText("");
            } else {
                alert("Failed to send message.");
            }
        } catch (e) {
            console.error("Error sending message:", e);
        } finally {
            setSendingMessage(false);
        }
    };

    const isVideo = (url: string) => /\.(mp4|webm|ogg|mov)$/i.test(url);

    if (!displayUser && loading) return <div className="loading" style={{color: "white"}}>Loading...</div>;
    if (!displayUser) return <div className="not-found" style={{color: "white"}}>User not found</div>;

    return (
        <div className="profile-page dark-mode">
            <div className="header">
                <button className="icon-button"><FontAwesomeIcon icon={faArrowLeft} onClick={() => {navigate(-1)}}/></button>
                <h1>{displayUser.username}</h1>
                {!isGuest && isMyProfile && (
                    <button className="icon-button" onClick={() => {navigate(`/profile/edit`)}}><FontAwesomeIcon icon={faCog} /></button>
                )}
            </div>

            <div className="profile-header">
                <div className="profile-pic-container">
                    <img src={displayUser.profilePictureUrl} alt="" className="profile-pic" />
                </div>
                <div className="profile-info">
                    <div className="stats">
                        <div className="stat"><span className="count">{posts.length}</span> postări</div>
                        <div className="stat"><span className="count">{displayUser.followersCount}</span> de urmăritori</div>
                        <div className="stat"><span className="count">{displayUser.followingCount}</span> de urmăriri</div>
                    </div>
                    <div className="bio">
                        <h2>{displayUser.name}</h2>
                        <p><FontAwesomeIcon icon={faGlobe} /> {displayUser.description}</p>
                        <p>{`@${displayUser.username}`}</p>
                    </div>
                </div>
            </div>

            {!isGuest && isMyProfile && (
                <div className="actions">
                    <button className="primary-button" onClick={() => {navigate(`/profile/edit`)}}>Edit profile</button>
                </div>
            )}

            {!isGuest && !isMyProfile && (
                <div className="actions">
                    <button className="primary-button" onClick={handleFollow}>
                        {doIFollow !== "None" ? (doIFollow === "Accepted" ? "Unfollow" : doIFollow) : "Follow"}
                    </button>
                    {/* UPDATED BUTTON: Opens Modal */}
                    <button className='primary-button' onClick={() => setIsMessageModalOpen(true)}>
                        Send Message
                    </button>
                </div>
            )}

            {/* --- NEW: MESSAGE MODAL --- */}
            {isMessageModalOpen && (
                <div className="modal-overlay-xx" onClick={() => setIsMessageModalOpen(false)}>
                    <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-header">
                            <h3>Message to {displayUser.username}</h3>
                            <button className="close-btn" onClick={() => setIsMessageModalOpen(false)}>
                                <FontAwesomeIcon icon={faTimes} />
                            </button>
                        </div>
                        <div className="modal-body">
                            <textarea 
                                className='text-input-modal'
                                placeholder="Type your message here..."
                                value={messageText}
                                onChange={(e) => setMessageText(e.target.value)}
                                rows={4}
                            />
                        </div>
                        <div className="modal-footer">
                            <button 
                                className="primary-button" 
                                onClick={handleSendMessage}
                                disabled={sendingMessage || !messageText.trim()}
                            >
                                {sendingMessage ? "Sending..." : "Send"} <FontAwesomeIcon icon={faPaperPlane} />
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* Privacy and No Posts Logic */}
            {userPrivacy === true && (
                <div className="no-posts-container"><span>This user is private</span></div>
            )}
            {userPrivacy === false && posts.length === 0 && (
                <div className="no-posts-container"><span>This user has no posts</span></div>
            )}
            
            {/* Grid Logic */}
            {userPrivacy === false && (
                <div className="photo-grid">
                    {posts.map(post => (
                        <Link 
                            to={`/p/${post.id}`} 
                            state={{ background: location }} 
                            key={post.id}
                        >
                            <div className="grid-item">
                                {isVideo(post.img_path) ? (
                                    <div className="video-thumbnail-container">
                                        <video src={post.img_path} muted preload="metadata" className="grid-media" />
                                    </div>
                                ) : (
                                    <img src={post.img_path} alt="Post" className="grid-media" />
                                )}
                            </div>
                        </Link>
                    ))}
                </div>
            )}
        </div>
    );
};

export default ProfilePage;