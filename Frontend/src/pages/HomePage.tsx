import PostContainer from '../components/PostContainer';

import '../styles/HomePage.css';

import { useNavigate } from 'react-router-dom';

function HomePage() {
    // const navigate = useNavigate()

    const circles = Array.from({length : 40}, (_, i) => {
        return {
            id: i,
            img_path: "./assets/img/download.jpg"
        }
    });

    return (
        <div className="home-page">

            {/* Story section if we decide to implement it */}
            {/* <section className="stories-container">
                {circles.map( circle => (
                <div key={circle.id} className="story">
                    <img src={circle.img_path} alt="" />
                </div>
                ))}
            </section> */}

            <PostContainer />
        </div>
    );
    

}

export default HomePage;
