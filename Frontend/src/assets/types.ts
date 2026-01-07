export interface UserSettingsType{
    userName: string,
    name: string,
    description: string,
    profile_image: string,
    privacy: boolean,
}





export interface UserProfileType{
    name: string,
    username: string,
    email: string,
    description: string
    profilePictureUrl: string,
    privacy: boolean,
    followingCount: number,
    followersCount: number
    posts: PostType[],
}

export interface UserProfileApiType{
    name: string,
    username: string,
    email: string,
    profilePictureUrl: string,
    privacy: boolean,
    followingCount: number,
    followersCount: number
    description: string,
}







export interface PostType{
    id: number,
    owner: string,
    img_path: string,
    description: string,
    nr_likes: number,
    has_liked: boolean,
    nr_comm: number,
    created: string,
    username: string,
    user_image_path: string,
}

export interface PostApiType {
    id: number;
    owner: string;
    image_path: string;
    description: string,
    nr_likes: number;
    has_liked : boolean,
    nr_comms: number; 
    created: string,
    username: string,
    user_image_path: string,
}




export interface UsersSearchApiType{
    username: string,
    name: string,
    profilePictureUrl: string,
}





export interface CommentApiType{
    id: number,
    content: string,
    createdAt: string,
    username: string,
    profilePictureUrl: string,
}

export interface CommentPostDTO{
    postId: number,
    content: string
}