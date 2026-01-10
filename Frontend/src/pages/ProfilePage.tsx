import '../styles/ProfilePage.css'; // Assuming a CSS file for styles

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCog, faArrowLeft, faGlobe } from '@fortawesome/free-solid-svg-icons';

import type { PostType, PostApiType, UserProfileType } from '../assets/types';



import { Link, useNavigate, useParams } from 'react-router-dom';
import { useCallback, useEffect, useState } from 'react';
import { useUser } from '../context/UserContext';


const ProfilePage = () => {
    const navigate = useNavigate()

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

    console.log(`User privacy: ${displayUser?.privacy === true} && !isMyProfile: ${!isMyProfile} && doIFollow: ${doIFollow}`)
    
    const userPrivacy = (displayUser?.privacy === true) &&  
                        !isMyProfile &&               
                        doIFollow !== "Accepted";

    console.log(userPrivacy)



    //// For the number of followers to change when i FOllow and Unfollow, i should call setDsiplayUser and change it's followingCount

    //fetching my User + Posts
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
                    if (!res.ok) {
                        throw new Error(`User ${usernamePath} not found: ${res.status}, ${res.statusText}`);
                    }
                    currentUserData = await res.json();
                    setDisplayUser(currentUserData);
                }

                if (!currentUserData) return;

                /// fetching posts
                let postsRes: Response;
                if (isMyProfile) {
                    postsRes = await fetch(`/api/Posts/my_posts`, {
                        headers: { 
                            'Authorization': `Bearer ${token}`,
                            'Content-Type': "application/json"
                        }
                    });
                } else {
                    
                    postsRes = await fetch(`/api/Posts/ByOwner/${usernamePath}`, {
                        headers: { 
                            'Authorization': `Bearer ${token}`,
                            'Content-Type': "application/json"
                        }
                    });
                }

                if (postsRes.ok) {
                    const data = await postsRes.json();
                   
                    
                    const transformedPosts = data.map((postData: PostApiType) => ({
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

    }, [usernamePath, contextUser, isMyProfile, doIFollow])
    ///maybe erase doIFollow if i dont want displayUser to change the followingCount


    /// see if i follow him
    useEffect(() => {
        if ( isGuest || isMyProfile ) return;

        console.log("ENTERED USEEFFECT SETDOIFOLLOW AND isMyProfile is false")

        const setFollow = async () => {
            const token = sessionStorage.getItem("userToken")
    
            try{
                const res = await fetch(`/api/Follow/status/${usernamePath}`, {
                    method: "GET",
                    headers: {
                        'Authorization': `Bearer ${token}`
                    }
                })

                if ( !res.ok){
                    throw new Error(`Response error: ${res.status}, ${res.statusText}`)
                }

                const data = await res.json();

                setDoIFollow(data.status);
    
            } catch (e){
                console.error("Error at follow check: ", e)
            }
        }

        setFollow()

    }, [isMyProfile, isGuest, usernamePath])



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

                console.log(doIFollow)
                console.log(`URL: /api/Follow/${doIFollow !== "None" ? "unfollow/" : ""}${usernamePath}`)
                console.log(`METHOD: ${doIFollow !== "None" ? "DELETE" : "POST"}`)

                if ( !res.ok ){
                    throw new Error(`${res.status}, ${res.statusText}`)
                }

                setDoIFollow(doIFollow === "None" ? `${!displayUser?.privacy ? "Accepted" : "Pending"}` : "None")

                console.log("Changed doIFollow to ", doIFollow)

            } catch(e){
                console.error("Follow error: ", e);
            }
            finally{
                console.log("");
            }
        }

        follow();
    }, [doIFollow, usernamePath, displayUser]);

    const isVideo = (url: string) => {
        return /\.(mp4|webm|ogg|mov)$/i.test(url);
    };


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
            {/* <div className="notification-badge">Bun si tu Ionute</div> */}
            </div>
            <div className="profile-info">
            <div className="stats">
                <div className="stat">
                <span className="count">{posts.length}</span> postări
                </div>
                <div className="stat">
                <span className="count">{displayUser.followersCount}</span> de urmăritori
                </div>
                <div className="stat">
                <span className="count">{displayUser.followingCount}</span> de urmăriri
                </div>
            </div>
            <div className="bio">
                <h2>
                    {/* {displayUser.name} */}
                    {displayUser.name}
                </h2>
                <p><FontAwesomeIcon icon={faGlobe} /> {displayUser.description}</p>
                <p>{`@${displayUser.username}`}</p>
            </div>
            </div>
        </div>

        {!isGuest && isMyProfile && (
            <div className="actions">
                <button className="primary-button" onClick={() => {navigate(`/profile/edit`)}}>Edit profile</button>
                {/* <button className="secondary-button">See archive</button> */}
            </div>
        )}
        {!isGuest && !isMyProfile && (
            <div className="actions">
                <button className="primary-button" onClick={handleFollow}>
                {
                    doIFollow !== "None" ? (doIFollow === "Accepted" ? "Unfollow" : doIFollow) : "Follow"
                }
                </button>
            </div>
        )}

        {userPrivacy === true && (
            <div className="no-posts-container">
                <span>This user is private</span>
            </div>
        )}
        {userPrivacy === false && posts.length === 0 && (
            <div className="no-posts-container">
                <span>This user has no posts</span>
            </div>
        )}
        
        {userPrivacy === false && (
            <div className="photo-grid">
                {posts.map(post => (
                    <Link 
                        to={`/p/${post.id}`} 
                        state={{ background: {
                            pathname: location.pathname,
                            search: location.search,
                            hash: location.hash,
                        } }} 
                        key={post.id}
                    >
                        <div className="grid-item">
                            {isVideo(post.img_path) ? (
                                <div className="video-thumbnail-container">
                                    <video 
                                        src={post.img_path} 
                                        muted 
                                        preload="metadata" // Loads first frame as thumbnail
                                        className="grid-media"
                                    />
                                    {/* Optional: Overlay icon to show it's a video */}
                                    <div className="video-icon-overlay">
                                        {/* <FontAwesomeIcon icon={faPlay} /> */}
                                    </div>
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