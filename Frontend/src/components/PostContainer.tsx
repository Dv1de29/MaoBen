import { useCallback, useEffect, useRef, useState } from "react";
import type { PostType, PostApiType } from "../assets/types";
import Post from "./Post";


interface PostContainerProsp{
    posts: PostType[],
}



// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
// REMEMBER THAT I MUST ADD THE USER INFO FOR EACH POST tO HAVE it UPPPP




function PostContainer(){
    const [posts, setPosts] = useState<PostType[]>([]);

    const [page, setPage] = useState<number>(1);
    const ITEMS_PER_PAGE = 7;

    const [loading, setLoading] = useState<boolean>(false);
    const [hasMore, setHasMore] = useState<boolean>(true);

    const observerTargetRef = useRef<HTMLDivElement>(null);
    const initialLoadRef = useRef(true);

    const fetchMyPosts = useCallback(async () => {
        if (loading || !hasMore) return;
        setLoading(true);

        const skipValue = (page - 1) * ITEMS_PER_PAGE;

        try{
            const res = await fetch(`http://localhost:5000/api/posts/?count=${ITEMS_PER_PAGE}&skip=${skipValue}`)
    
            if ( !res.ok ){
                if ( res.status === 204 || res.status === 404 || res.status === 400){
                    setHasMore(false)
                    setLoading(false)
                    return;
                }
                throw new Error(`Response error: ${res.status},${res.statusText}`)
            }

            const data: PostApiType[] = await res.json();

            if (data.length === 0 || data.length < ITEMS_PER_PAGE) {
                setHasMore(false);
            }

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

            setPosts(prev => [...prev, ...transformedPosts]);
            setPage(prev => prev+1);

        } catch(e){
            console.error("Error at loading my posts: ", e)
        } finally{
            setLoading(false);
        }
        

    }, [loading, hasMore, page])

    //fetching first set
    useEffect(() => {
        if ( initialLoadRef.current){
            initialLoadRef.current = false;
            fetchMyPosts();
        }
    }, [fetchMyPosts])

    // fetching on intersecting observer
    useEffect(() => {
        if ( !observerTargetRef.current || !hasMore ) return;

        const ObserverCall = (entries: IntersectionObserverEntry[]) => {
            const target = entries[0];
            if ( target.isIntersecting && !loading && posts.length > 0 && hasMore ){
                fetchMyPosts();
            }
        }

        const observer = new IntersectionObserver(ObserverCall, {
            root: null,
            rootMargin: '0px',
            threshold: 1.0,
        });

        const currentTarget = observerTargetRef.current;
        if (currentTarget) {
            observer.observe(currentTarget);
        }

        return () => {
            observer.disconnect();
        }

    }, [loading, hasMore, fetchMyPosts, posts.length]);


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
            {hasMore ? (
                <div className="observer" ref={observerTargetRef} style={{height: 20, textAlign: 'center'}}>
                    {loading ? 'Loading...' : 'Scroll down'}
                </div>
            ) : (
                <div style={{ textAlign: 'center', padding: '10px', color: '#888' }}>
                    --- End of Posts ---
                </div>
            )}
        </section>
    )
}

export default PostContainer;