import { useCallback, useEffect, useState } from "react";
import type { PostType, PostApiType } from "../assets/types";
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

    

    useEffect(() => {
        const fetchMyPosts = async () => {
            try{
                const res = await fetch("http://localhost:5000/api/posts/?count=10")
    
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

        fetchMyPosts();
    }, [])


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