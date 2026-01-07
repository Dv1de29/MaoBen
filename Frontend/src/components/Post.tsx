import { memo, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmarkCircle, faBookmark, faComment, faHeart } from '@fortawesome/free-solid-svg-icons';// Using regular for outline style
import { faHeart as faHeartSolid } from '@fortawesome/free-solid-svg-icons'; // Solid for liked state

import type { CommentApiType, CommentPostDTO, PostType } from '../assets/types';
import '../styles/Post.css';

interface PostProps {
    post: PostType;
    onToggleLike: (id: number, likeState: boolean) => void;
}

const Post = memo(({ post, onToggleLike }: PostProps) => {
    const navigate = useNavigate();
    const myusername = sessionStorage.getItem("userName");

    const [isLiked, setIsLiked] = useState<boolean>(post.has_liked);
    const [showComments, setShowComments] = useState<boolean>(false);
    const [displayComments, setDisplayComments] = useState<CommentApiType[]>([]);
    const [inputCommentValue, setInputCommentValue] = useState<string>("");

    const debounceTimer = useRef<number | null>(null);
    const pendingSave = useRef<{ id: number, state: boolean } | null>(null);

    // --- Logic Section (Unchanged) ---
    useEffect(() => {
        return () => {
            if (debounceTimer.current) {
                clearTimeout(debounceTimer.current);
                if (pendingSave.current) {
                    const args = pendingSave.current;
                    onToggleLike(args.id, args.state);
                }
            }
        };
    }, [onToggleLike]);

    const handleLike = () => {
        const newLikeState = !isLiked;
        setIsLiked(newLikeState);
        pendingSave.current = { id: post.id, state: newLikeState };
        if (debounceTimer.current) clearTimeout(debounceTimer.current);
        debounceTimer.current = window.setTimeout(() => {
            onToggleLike(post.id, newLikeState);
            pendingSave.current = null;
            debounceTimer.current = null;
        }, 1000);
    };

    const fetchComments = async (post_id: number) => {
        const token = sessionStorage.getItem("userToken");
        try {
            const res = await fetch(`/api/Comments/${post_id}`, { headers: { 'Authorization': `Bearer ${token}` } });
            if (!res.ok) throw new Error(`Response Error`);
            const data: CommentApiType[] = await res.json();
            setDisplayComments(data);
        } catch (e) { console.error(e); }
    };

    const toggleComments = () => {
        if (!showComments && displayComments.length === 0) fetchComments(post.id);
        setShowComments(prev => !prev);
    };

    const handlePostComment = async () => {
        const token = sessionStorage.getItem("userToken");
        const commentPayload: CommentPostDTO = { postId: post.id, content: inputCommentValue };
        try {
            const res = await fetch(`/api/Comments`, {
                method: "POST",
                headers: { 'Authorization': `Bearer ${token}`, "Content-Type": "application/json" },
                body: JSON.stringify(commentPayload)
            });
            if (!res.ok) throw new Error(`Response Error`);
            const data: CommentApiType = await res.json();
            const newComment: CommentApiType = {
                id: data.id, content: data.content, createdAt: data.createdAt,
                username: data.username, profilePictureUrl: data.profilePictureUrl,
            };
            setDisplayComments(prev => [...prev, newComment]);
            setInputCommentValue("");
        } catch (e) { console.error(e); }
    };

    function getTimeAgo(dateString: string): string {
        const date = new Date(dateString);
        const now = new Date();
        const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);
        if (seconds < 60) return `${seconds}s`; // Shortened for design match
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return `${minutes}m`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `${hours}h`;
        const days = Math.floor(hours / 24);
        return `${days}d`;
    }

    const handleDeleteComment = (comment_id: number) => {
        const token = sessionStorage.getItem("userToken");
        fetch(`/api/Comments/${comment_id}`, { method: "DELETE", headers: { 'Authorization': `Bearer ${token}` } });
        setDisplayComments(prev => prev.filter(c => c.id !== comment_id));
    };

    // --- Render Section ---
    return (
        <article className="post-container">
            {/* 1. Header: Avatar + Name + Bookmark */}
            <header className="post-header">
                <div className="header-left">
                    <img 
                        className="post-avatar"
                        src={post.user_image_path} 
                        alt={post.username} 
                        onClick={() => navigate(`/profile/${post.username}`)}
                    />
                    <div className="header-info">
                        <div className="user-row">
                            <span className="username" onClick={() => navigate(`/profile/${post.username}`)}>
                                {post.username}
                            </span>
                            <span className="user-handle">@{post.username.toLowerCase()}</span>
                            <span className="separator">·</span>
                            <span className="time-ago">{getTimeAgo(post.created)}</span>
                        </div>
                    </div>
                </div>
                {/* Bookmark pushed to top right */}
                {/* <button className="icon-btn bookmark-btn">
                    <FontAwesomeIcon icon={faBookmark} />
                </button> */}
            </header>

            {/* 2. Content Text (Moved ABOVE Image) */}
            <div className="post-text-content">
                {post.description}
            </div>

            {/* 3. Media */}
            <div className="post-media" onDoubleClick={handleLike}>
                <img src={post.img_path} alt="Post content" />
            </div>

            {/* 4. Footer: Action Bar (Icons + Counts) */}
            <div className="post-footer">
                <div className="action-bar">
                    {/* Left Actions: Like & Comment */}
                    <div className="action-group">
                        <button className={`action-item ${isLiked ? 'liked' : ''}`} onClick={handleLike}>
                            <FontAwesomeIcon icon={isLiked ? faHeartSolid : faHeart} />
                            <span className="action-count">{post.nr_likes + (isLiked && !post.has_liked ? 1 : (!isLiked && post.has_liked ? -1 : 0))}</span>
                        </button>

                        <button className="action-item" onClick={toggleComments}>
                            <FontAwesomeIcon icon={faComment} />
                            <span className="action-count">{post.nr_comm}</span>
                        </button>
                    </div>

                </div>

                {/* Drawer / Comments Section (Kept below actions) */}
                <div className={`comments-drawer ${showComments ? 'open' : ''}`}>
                    <div className="comments-list">
                        {displayComments.map((comment) => (
                            <div key={comment.id} className="comment-item">
                                <div className="comment-user-info">
                                    <img src={comment.profilePictureUrl} alt="" onClick={() => navigate(`/profile/${comment.username}`)}/>
                                    <span className="comment-user" onClick={() => navigate(`/profile/${comment.username}`)}>{comment.username}</span>
                                </div>
                                <span className="comment-text">{comment.content}</span>
                                {(myusername === comment.username || myusername === post.username) && (
                                    <div className='delete-comment-icon' onClick={() => handleDeleteComment(comment.id)}>
                                        <FontAwesomeIcon icon={faXmarkCircle} />
                                    </div>
                                )}
                            </div>
                        ))}
                        {displayComments.length === 0 && <p className="no-comments">No comments yet.</p>}
                    </div>
                </div>

                {/* Input Section (Shows when comments are toggled or always visible if you prefer) */}
                {showComments && (
                    <div className="add-comment-wrapper">
                        <input 
                            type="text" 
                            placeholder="Add a comment..."
                            value={inputCommentValue}
                            onChange={(e) => setInputCommentValue(e.target.value)}
                        />
                        {inputCommentValue.trim().length > 0 && (
                            <button className="post-text-btn" onClick={handlePostComment}>Post</button>
                        )}
                    </div>
                )}
            </div>
        </article>
    );
});

export default Post;