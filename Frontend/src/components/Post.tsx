

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

    return(
        <div className="card">
            <div className="header">
                <img className="profile-pic" src={post.img_path} alt="Avatar" /> 
        
                <span className="username">{post.owner}</span> 
            </div>
            <img src={post.img_path} alt="" />                   
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