import { useCallback, useState } from "react";
import type { PostType } from "../assets/types";
import Post from "./Post";


interface PostContainerProsp{
    posts: PostType[],
}

function PostContainer(){
    const [posts, setPosts] = useState<PostType[]>(Array.from({length: 1000}, (_, i) => {
        if ( i % 2 == 0 ){
            return{
                id: i,
                owner: "Ben",
                img_path: "../assets/img/ben1.jpg",
                nr_likes: 40,
                has_liked: true,
                nr_comm: 10, 
            }
        } else {
            return {
                id: i,
                owner: "Mr_Orange",
                img_path: "../assets/img/download.jpg",
                nr_likes: 40,
                has_liked: true,
                nr_comm: 10,
            }
        }
    }));


    const handleLike = useCallback((id: number) => {
        setPosts(prev => prev.map(p => {
            if ( p.id === id ){
                return{
                    ...p,
                    has_liked: true,
                }
            }
            return p;
        }))
    }, [])

    return(
        <section className="card-container">
            {posts.map(post => (
                <div className="card_wrapper" key={post.id}>
                    <Post
                        key={post.id}
                        post={post}
                        onToggleLike={handleLike}
                    />
                </div>
            ))}
        </section>
    )
}

export default PostContainer;