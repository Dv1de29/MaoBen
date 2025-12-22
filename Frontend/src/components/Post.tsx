

import type { CommentApiType, CommentPostDTO, PostType } from '../assets/types';

import HeartIcon from '../assets/svg/heart.svg?react'
import CommIcon from '../assets/svg/comm-icon.svg'

import { memo, useEffect, useRef, useState } from 'react';

import '../styles/Post.css'
import { useNavigate } from 'react-router-dom';


import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmarkCircle } from '@fortawesome/free-solid-svg-icons';



interface PostProps{
    post: PostType;
    onToggleLike: (id: number, likeState: boolean) => void,
}

const Post = memo(({post, onToggleLike}: PostProps) => {
    const navigate = useNavigate();

    const myusername = sessionStorage.getItem("userName")

    const [isLiked, setIsLiked] = useState<boolean>(post.has_liked);
    const [showComments, setShowComments] = useState<boolean>(false);

    const [displayComments, setDisplayComments] = useState<CommentApiType[]>([])

    const [inputCommentValue, setInputCommentValue] = useState<string>("")


    // Timer for npt spamming like
    const debounceTimer = useRef<number | null>(null);

    // Save state for fixing not calling function if i refresh before the Timer
    const pendingSave = useRef<{ id: number, state: boolean } | null>(null);


    //// unmount return for debounceTimer erase
    useEffect(() => {
        return () => {
            if (debounceTimer.current) {
                clearTimeout(debounceTimer.current);

                if ( pendingSave.current ){
                    const args = pendingSave.current;
                    console.log("Flushing pending on unmount")
                    onToggleLike(args.id, args.state)
                }
            }
        };
    }, [onToggleLike]);


    /// handleLike method for liking/unliking
    const handleLike = () => {
        const newLikeState = !isLiked

        setIsLiked(newLikeState);

        pendingSave.current = { id: post.id, state: newLikeState };
        
        if ( debounceTimer.current ) clearTimeout(debounceTimer.current)
            
            debounceTimer.current = setTimeout(() => {
                console.log("Calling API for postid: ", post.id, "with state: ", newLikeState)
                
                // Change this with API
                onToggleLike(post.id, newLikeState);

                pendingSave.current = null; 
                debounceTimer.current = null;
        }, 1000)
    }


    /// fetching comments function
    const fetchComments = async (post_id: number) => {
        const token = sessionStorage.getItem("userToken")

        try{
            const res = await fetch(`/api/Comments/${post_id}`, {
                headers: {
                    'Authorization': `Bearer ${token}` 
                }
            })

            if ( !res.ok ){
                throw new Error(`Response Error: ${res.status}, ${res.statusText}`)
            }

            const data: CommentApiType[] = await res.json();

            setDisplayComments(data)
        } catch(e){
            console.error("Error at fetcghing comments: ", e);
        }
    }

    //// toggleComments drawer and fetch if first time
    const toggleComments = () => {
        if ( !showComments && displayComments.length === 0){
            fetchComments(post.id);
        }

        setShowComments(prev => !prev)
    }


    ////// sending a comment
    const handlePostComment = async () => {
        const token = sessionStorage.getItem("userToken");

        const commentPayload: CommentPostDTO = {
            postId: post.id,
            content: inputCommentValue,
        };

        try {
            const res = await fetch(`/api/Comments`, {
                method: "POST",
                headers: {
                    'Authorization': `Bearer ${token}`,
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(commentPayload)
            });

            if (!res.ok) throw new Error(`Response Error: ${res.status}, ${res.statusText}`);

            const data: CommentApiType = await res.json();

            console.log(data)

            const newComment: CommentApiType = {
                id: data.id,
                content: data.content,
                createdAt: data.createdAt,
                username: data.username,
                profilePictureUrl: data.profilePictureUrl,
            } 

            setDisplayComments(prev => [...prev, newComment]);

            setInputCommentValue(""); 

        } catch (e) {
            console.error("Error at posting comment", e);
        }

        setInputCommentValue("")
    }


    // fucntions for converting time string into [TIME AGO]
    function getTimeAgo(dateString: string): string {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

        if (seconds < 5) return 'Just now'; 

        const intervals = [
            { label: 'year', seconds: 31536000 },
            { label: 'month', seconds: 2592000 },
            { label: 'day', seconds: 86400 },
            { label: 'hour', seconds: 3600 },
            { label: 'minute', seconds: 60 }
        ];

        for (const interval of intervals) {
            const count = Math.floor(seconds / interval.seconds);
            if (count >= 1) {
                // Handle pluralization (e.g., "1 day" vs "3 days")
                return `${count} ${interval.label}${count !== 1 ? 's' : ''} ago`;
            }
        }

        return 'Just now';
    }

    ///handleDeletingComment
    const handleDeleteComment = (comment_id: number) => {
        const token = sessionStorage.getItem("userToken");

        console.log("DELETING COMMNENT: ", comment_id);

        fetch(`/api/Comments/${comment_id}`, {
            method: "DELETE",
            headers: {
                'Authorization': `Bearer ${token}`,
            }
        })
        .catch((e) => {
            console.error("Error at deleting comment: ", e);
            return;
        })

        setDisplayComments(prev => prev.filter(c => c.id !== comment_id))
    }


    return(
        <div className="card">
            <div className="header">
                <img className="profile-pic" src={post.user_image_path} alt="Avatar" onClick={() => {
                    navigate(`/profile/${post.username}`)
                }}/> 
        
                <span className="username" onClick={() => {
                    navigate(`/profile/${post.username}`)
                }}>{post.username}</span> 
            </div>
            <img src={post.img_path} alt="" onDoubleClick={() => {handleLike()}}/>  
            <span className='date-created'>{getTimeAgo(post.created)}</span>            
            <span className='description'>
                <span className='description-owner'>
                    <img src={post.user_image_path} alt="" />
                    <span>{post.username}</span>
                </span>
                <span className='description-content'>{post.description}</span>
            </span>
            <div className="reactions">
                <button style={{backgroundColor: "transparent", border: "none"}}>
                    <HeartIcon 
                        className='react-icons'
                        fill={isLiked === true ? "red" : "none"}
                        onClick={handleLike}
                    />
                </button>
                <button style={{ backgroundColor: "transparent", border: "none" }} onClick={toggleComments}>
                    <img className='react-icons' src={CommIcon} alt="comments" />
                </button>
            </div>

            <div className={`comments-drawer ${showComments ? 'open' : ''}`}>
                <div className="comments-list">
                    {displayComments.map((comment) => (
                        <div key={comment.id} className="comment-item">
                            <div className="comment-user-info">
                                <img src={comment.profilePictureUrl} alt="" />
                                <span className="comment-user">{comment.username}</span>
                            </div>
                            <span className="comment-text">{comment.content}</span>
                            {(myusername === comment.username || myusername === post.username) && (
                                <FontAwesomeIcon className='delete-comment' icon={faXmarkCircle} onClick={() => handleDeleteComment(comment.id)}/>
                            )}
                        </div>
                    ))}
                    {displayComments.length === 0 && <p className="no-comments">No comments yet.</p>}
                </div>

                <div className="comment-input-wrapper">
                    <input 
                        type="text" 
                        placeholder="Add a comment..."
                        value={inputCommentValue}
                        onChange={(e) => {setInputCommentValue(e.target.value)}} 
                    />
                    <button className="post-btn" onClick={handlePostComment}>Post</button>
                </div>
            </div>
        </div>
    );
})  ;

export default Post;