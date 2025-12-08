import '../styles/ProfilePage.css'; // Assuming a CSS file for styles

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCog, faArrowLeft, faGlobe, faPlus, faTh, faBookmark, faUserTag } from '@fortawesome/free-solid-svg-icons';

import type { UserProfileType } from '../assets/types';



import profilePic from '../assets/images/download.jpg'; // Replace with actual images
import highlight1 from '../assets/images/download.jpg';
import highlight2 from '../assets/images/download.jpg';
import highlight3 from '../assets/images/download.jpg';
import highlight4 from '../assets/images/download.jpg';

import { useNavigate } from 'react-router-dom';





const ProfilePage = () => {
    const navigate = useNavigate()


    const user: UserProfileType = {
        id: 1,
        userName: "david._florian",
        name: "David",
        description: "YNWA",
        nr_followers: 700,
        nr_following: 600,
        posts: [{
            id: 1,
            owner: "david._florian",
            img_path: "./assets/img/download.jpg",
            nr_likes: 10000,
            has_liked: true,
            nr_comm: 10,
            },
            {
            id: 2,
            owner: "david._florian",
            img_path: "./assets/img/download.jpg",
            nr_likes: 10000,
            has_liked: true,
            nr_comm: 10,
            }
        ],
    }


    return (
        <div className="profile-page dark-mode">
        <div className="header">
            <button className="icon-button"><FontAwesomeIcon icon={faArrowLeft} onClick={() => {navigate(-1)}}/></button>
            <h1>{user.userName}</h1>
            <button className="icon-button" onClick={() => {navigate(`edit`)}}><FontAwesomeIcon icon={faCog} /></button>
        </div>

        <div className="profile-header">
            <div className="profile-pic-container">
            <img src={profilePic} alt="" className="profile-pic" />
            <div className="notification-badge">Bun si tu Ionute</div>
            </div>
            <div className="profile-info">
            <div className="stats">
                <div className="stat">
                <span className="count">{user.posts.length}</span> postări
                </div>
                <div className="stat">
                <span className="count">{user.nr_followers}</span> de urmăritori
                </div>
                <div className="stat">
                <span className="count">{user.nr_following}</span> de urmăriri
                </div>
            </div>
            <div className="bio">
                <h2>{user.name}</h2>
                <p><FontAwesomeIcon icon={faGlobe} /> {user.description}</p>
                <p>{`@${user.userName}`}</p>
            </div>
            </div>
        </div>

        <div className="actions">
            <button className="primary-button">Edit profile</button>
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

        <div className="photo-grid">
            {user.posts.map(post => (
                <>
                    <div className="grid-item"><img src={post.img_path} alt="Post 1" /></div>
                </>
            ))}
        </div>
        </div>
    );
};

export default ProfilePage;