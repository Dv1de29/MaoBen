import PostContainer from '../components/PostContainer';

import '../styles/HomePage.css';

import type { PostType } from '../assets/types';

function HomePage() {
    const circles = Array.from({length : 40}, (_, i) => {
        return {
            id: i,
            img_path: "./assets/img/download.jpg"
        }
    });

    // const posts = Array.from({length: 1000}, (_, i) => {
    //     return {
    //         userName: "Mr_Orange",
    //         img_path: "./assets/img/download.jpg",
    //         liked: true,
    //     }
    // })

    return (
        <div className="home-page">
            <section className="stories-container">
                {circles.map( circle => (
                <div key={circle.id} className="story">
                    <img src={circle.img_path} alt="" />
                </div>
                ))}
            </section>

            <PostContainer />
        </div>
    );
    

}

export default HomePage;
