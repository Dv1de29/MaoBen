export interface PostType{
    id: number,
    owner: string,
    img_path: string,
    nr_likes: number,
    has_liked: boolean,
    nr_comm: number,
}

export interface UserSettingsType{
    id: number,
    userName: string,
    name: string,
    description: string,
    profile_image: string,
}

export interface UserProfileType{
    id: number,
    userName: string,
    name: string,
    description: string
    nr_followers: number,
    nr_following: number,
    posts: PostType[],
}