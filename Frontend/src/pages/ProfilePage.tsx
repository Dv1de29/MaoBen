import '../styles/ProfilePage.css'; // Assuming a CSS file for styles

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCog, faArrowLeft, faGlobe, faPlus, faTh, faBookmark, faUserTag } from '@fortawesome/free-solid-svg-icons';

import type { PostType, PostApiType, UserProfileType, UserProfileApiType } from '../assets/types';



import profilePic from '../assets/images/download.jpg'; // Replace with actual images
import highlight1 from '../assets/images/download.jpg';
import highlight2 from '../assets/images/download.jpg';
import highlight3 from '../assets/images/download.jpg';
import highlight4 from '../assets/images/download.jpg';

import { useNavigate, useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';



/// A GOOD IMPLEMENTATION WOULD BE A GLOBAL STATE INSIDE A PROVIDER THAT HOLDS THE USER


const ProfilePage = () => {
    const navigate = useNavigate()

    const { usernamePath } = useParams();

    const initial_user: UserProfileType = {
        username: "",
        email: "",
        privacy: false,
        profilePictureUrl: "",
        // name: "David",
        description: "",
        // nr_followers: 700,
        // nr_following: 600,
        posts: [],
    }

    const [posts, setPosts] = useState<PostType[]>([]);
    const [user, SetUser] = useState<UserProfileType>(initial_user)

    //fetching my User + Posts
    useEffect(() => {
        const fetchMyPosts = async () => {
            try{
                const res = await fetch(`/api/posts/ByOwner/1`)
        
                if ( !res.ok ){
                    throw new Error(`Response error: ${res.status},${res.statusText}`)
                }
    
                const data = await res.json();
    
                const transformedPosts = data.map((postData: PostApiType) => {
                    return{
                        id: postData.id,
                        owner: postData.owner,
                        img_path: postData.image_path,
                        nr_likes: postData.nr_likes,
                        nr_comm: postData.nr_comms,
                        has_liked: false,
                    }
                });
    
                setPosts(transformedPosts);
    
            } catch(e){
                console.error("Error at loading my posts: ", e)
            }
        }

        const fetchUser = async () => {
            try{
                const token = sessionStorage.getItem("userToken");

                const res = await fetch("/api/Profile", {
                    method: 'GET',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    },
                })

                if ( !res.ok ){
                    throw new Error(`User error: ${res.status}, ${res.statusText}`);
                }

                const data: UserProfileApiType = await res.json();

                console.log(data)

                SetUser({
                    username: data.username,
                    email: data.email,
                    profilePictureUrl: data.profilePictureUrl,
                    privacy: data.privacy,
                    description: data.description,
                    posts: [],
                })
                

            } catch(e){
                console.log("Error at fetching user: ", e);
            }
        }

        // const uid = sessionStorage.getItem("userId");

        // if ( !uid ){
        //     console.error("NO USER ID");
        //     return;
        // }

        fetchUser()
        fetchMyPosts(); 

    }, [])


    return (
        <div className="profile-page dark-mode">
        <div className="header">
            <button className="icon-button"><FontAwesomeIcon icon={faArrowLeft} onClick={() => {navigate(-1)}}/></button>
            <h1>{user.username}</h1>
            {!usernamePath && (
                <button className="icon-button" onClick={() => {navigate(`edit`)}}><FontAwesomeIcon icon={faCog} /></button>
            )}
        </div>

        <div className="profile-header">
            <div className="profile-pic-container">
            <img src={user.profilePictureUrl} alt="" className="profile-pic" />
            <div className="notification-badge">Bun si tu Ionute</div>
            </div>
            <div className="profile-info">
            <div className="stats">
                <div className="stat">
                <span className="count">{posts.length}</span> postări
                </div>
                <div className="stat">
                <span className="count">{900}</span> de urmăritori
                </div>
                <div className="stat">
                <span className="count">{200}</span> de urmăriri
                </div>
            </div>
            <div className="bio">
                <h2>
                    {/* {user.name} */}
                    {"David"}
                </h2>
                <p><FontAwesomeIcon icon={faGlobe} /> {user.description}</p>
                <p>{`@${user.username}`}</p>
            </div>
            </div>
        </div>

        <div className="actions">
            <button className="primary-button" onClick={() => {navigate(`edit`)}}>Edit profile</button>
            <button className="secondary-button">See archive</button>
        </div>

        {/* <div className="highlights">
            <div className="highlight">
            <div className="highlight-img-container"><img src={highlight1} alt="Squirrel" /></div>
            <p>❤️🐿️</p>
            </div>
            <div className="highlight">
            <div className="highlight-img-container"><img src={highlight2} alt="Night" /></div>
            <p>🤪</p>
            </div>
            <div className="highlight">
            <div className="highlight-img-container"><img src={highlight3} alt="Dog" /></div>
            <p>🐶 maestrul 🐶</p>
            </div>
            <div className="highlight">
            <div className="highlight-img-container"><img src={highlight4} alt="Fans" /></div>
            <p>🐶 💙 🐶</p>
            </div>
            <div className="highlight">
            <div className="highlight-img-container add-new"><FontAwesomeIcon icon={faPlus} /></div>
            <p>Nou</p>
            </div>
        </div> */}

        <div className="tabs">
            <button className="tab active"><FontAwesomeIcon icon={faTh} /></button>
            <button className="tab"><FontAwesomeIcon icon={faBookmark} /></button>
            <button className="tab"><FontAwesomeIcon icon={faUserTag} /></button>
        </div>

            {posts.length === 0 && (
                <div className="no-posts-container">
                    <span>This user has no posts</span>
                </div>
            )}
        <div className="photo-grid">
            {posts.map(post => (
                <div className="grid-item" key={post.id}><img src={post.img_path} alt="Post 1" /></div>
            ))}
        </div>
        </div>
    );
};

export default ProfilePage;