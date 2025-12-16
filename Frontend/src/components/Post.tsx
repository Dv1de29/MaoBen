

import type { PostType } from '../assets/types';

import HeartIcon from '../assets/svg/heart.svg?react'
import CommIcon from '../assets/svg/comm-icon.svg'
import { memo, useState } from 'react';

import '../styles/Post.css'

interface PostProps{
    post: PostType;
    onToggleLike: (id: number) => void,
}

const Post = memo(({post, onToggleLike}: PostProps) => {
    const [isLiked, setIsLiked] = useState<boolean>(post.has_liked);

    const handleLike = () => {
        setIsLiked(!isLiked);
        onToggleLike(post.id);
    }

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

    return(
        <div className="card">
            <div className="header">
                <img className="profile-pic" src={post.img_path} alt="Avatar" /> 
        
                <span className="username">{post.username}</span> 
            </div>
            <img src={post.img_path} alt="" />  
            <span className='date-created'>{getTimeAgo(post.created)}</span>            
            <div className="reactions">
                <button style={{backgroundColor: "transparent", border: "none"}}>
                    <HeartIcon 
                        className='react-icons'
                        fill={isLiked ? "red" : "none"}
                        onClick={handleLike}
                    />
                </button>
                <img className='react-icons' src={CommIcon} alt="" />
            </div>
        </div>
    );
})  ;

export default Post;