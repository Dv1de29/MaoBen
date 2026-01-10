import { useCallback, useEffect, useRef, useState } from "react";
import type { PostType, PostApiType } from "../assets/types";
import Post from "./Post";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faCamera } from "@fortawesome/free-solid-svg-icons";


interface PostContainerProsp{
    posts: PostType[],
}


const SkeletonPost = () => (
    <div className="skeleton-wrapper">
        <div className="skeleton-header">
            <div className="skeleton-avatar shim"></div>
            <div className="skeleton-text shim"></div>
        </div>
        <div className="skeleton-image shim"></div>
        <div className="skeleton-footer shim"></div>
    </div>
);


function PostContainer(){
    const [posts, setPosts] = useState<PostType[]>([]);

    const [page, setPage] = useState<number>(1);
    const ITEMS_PER_PAGE = 7;

    const [loading, setLoading] = useState<boolean>(false);
    const [initialLoading, setInitialLoading] = useState<boolean>(true);
    const [hasMore, setHasMore] = useState<boolean>(true);

    const isGeust = sessionStorage.getItem("userRole") === "Guest";

    const observerTargetRef = useRef<HTMLDivElement>(null);
    const initialLoadRef = useRef(true);

    console.log(isGeust)

    const fetchMyPosts = useCallback(async () => {
        console.log("Entered fetched posts")
        if (loading || !hasMore) return;
        setLoading(true);

        const skipValue = (page - 1) * ITEMS_PER_PAGE;

        const token = sessionStorage.getItem("userToken");
// ${isGeust ? "" : "/feed"}
        try{
            const res = await fetch(`/api/Posts${isGeust ? "" : "/feed"}?count=${ITEMS_PER_PAGE}&skip=${skipValue}`, {
                headers:{
                    'Authorization': `Bearer ${token}`
                }
            })
    
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

            console.log(data)
            const transformedPosts = data.map((postData: PostApiType) => {
                return{
                    id: postData.id,
                    owner: postData.owner,
                    img_path: postData.image_path,
                    description: postData.description,
                    nr_likes: postData.nr_likes,
                    nr_comm: postData.nr_comms,
                    has_liked: postData.has_liked,
                    created: postData.created,
                    username: postData.username,
                    user_image_path: postData.user_image_path ? postData.user_image_path : "/assets/img/no_user.png",
                }
            });

            setPosts(prev => [...prev, ...transformedPosts]);
            setPage(prev => prev+1);

        } catch(e){
            console.error("Error at loading my posts: ", e)
        } finally{
            setLoading(false);
            setInitialLoading(false);
        }
        

    }, [loading, hasMore, page])


    const fetchAnyPosts = useCallback(async () => {
        console.log("Entered fetched posts")
        setLoading(true);

        const skipValue = (page - 1) * ITEMS_PER_PAGE;


        try{
            const res = await fetch(`/api/Posts/?count=${ITEMS_PER_PAGE}&skip=${0}`)
    
            if ( !res.ok ){
                if ( res.status === 204 || res.status === 404 || res.status === 400){
                    setHasMore(false)
                    setLoading(false)
                    return;
                }
                throw new Error(`Response error: ${res.status},${res.statusText}`)
            }

            console.log(res)

            const data: PostApiType[] = await res.json();

            if (data.length === 0 || data.length < ITEMS_PER_PAGE) {
                setHasMore(false);
            }

            console.log(data)
            const transformedPosts = data.map((postData: PostApiType) => {
                return{
                    id: postData.id,
                    owner: postData.owner,
                    img_path: postData.image_path,
                    description: postData.description,
                    nr_likes: postData.nr_likes,
                    nr_comm: postData.nr_comms,
                    has_liked: postData.has_liked,
                    created: postData.created,
                    username: postData.username,
                    user_image_path: postData.user_image_path ? postData.user_image_path : "/assets/img/no_user.png",
                }
            });

            setPosts(prev => [...prev, ...transformedPosts]);
            setPage(prev => prev+1);

        } catch(e){
            console.error("Error at loading my posts: ", e)
        } finally{
            setLoading(false);
            setInitialLoading(false);
        }
        

    }, [loading, hasMore, page])


    //fetching first set
    useEffect(() => {
        if ( initialLoadRef.current ){
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
            rootMargin: '200px',
            threshold: 0.1,
        });

        const currentTarget = observerTargetRef.current;
        if (currentTarget) {
            observer.observe(currentTarget);
        }

        return () => {
            observer.disconnect();
        }

    }, [loading, hasMore, fetchMyPosts, posts.length]);


    const handleLike = useCallback((id: number, likeState: boolean) => {
        const token = sessionStorage.getItem("userToken")

        setPosts(prev => prev.map(p => {
            if ( p.id === id ){
                let newCount = p.nr_likes;
                
                // Safety check: Don't double count if state is already consistent
                if (likeState && !p.has_liked) {
                    newCount++;
                } else if (!likeState && p.has_liked) {
                    newCount--;
                }
                return{
                    ...p,
                    has_liked: likeState,
                    nr_likes: newCount < 0 ? 0 : newCount,
                }
            }
            return p;
        }))

        fetch(`/api/Likes/toggle/${id}?likeState=${likeState}`, {
            method: "POST",
            keepalive: true, // Critical for refresh
            headers: {
                'Authorization': `Bearer ${token}`,
            }
        }).catch(e => {
            console.error("HandleLike Error: ", e);
            // Optional: Revert UI state here if needed
        });

    }, [])

    if ( posts.length <= 0 ) return (
        <>
            <div className="feed-layout">
                <span className="no-posts">{"No posts found"}</span>
                <br></br>
                <span className="no-posts">Start following poeple to see their posts</span>
                <br></br>
                <div className="no-posts-icon-wrapper">
                    <FontAwesomeIcon className="no-posts-icon" icon={faCamera}/>
                </div>
                <button className="btn-fetchAny" onClick={fetchAnyPosts}>
                    <span>Or see other posts</span>
                </button>
            </div>
        </>
    )

    return(
        <div className="feed-layout">

            {/* Posts Feed */}
            <section className="feed-stream">
                {posts.map(post => (
                    <article className="post-wrapper" key={post.id}>
                        <Post 
                            post={post} 
                            onToggleLike={handleLike} 
                        />
                    </article>
                ))}

                {/* Loading State */}
                <div ref={observerTargetRef} className="loading-trigger">
                    {(initialLoading || loading) && hasMore && (
                        <>
                            <SkeletonPost />
                            <SkeletonPost />
                        </>
                    )}
                </div>

                {!hasMore && posts.length > 0 && (
                    <div className="end-of-feed">
                        <div className="check-icon">✓</div>
                        <p>You're all caught up</p>
                    </div>
                )}
            </section>
        </div>
    )
}

export default PostContainer;