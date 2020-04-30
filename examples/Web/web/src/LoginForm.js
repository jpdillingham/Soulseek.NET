import React, { Component } from 'react'
import { Button, Form, Grid, Header, Icon, Segment, Checkbox } from 'semantic-ui-react'

const initialState = {
    username: '',
    password: '',
    rememberMe: true
}

class LoginForm extends Component {
    state = initialState;

    handleChange = (field, value) => {
        this.setState({
            [field]: value,
        });
    }

    render = () => {
        const { onLoginAttempt } = this.props;
        const { username, password, rememberMe } = this.state;

        return (
            <Grid textAlign='center' style={{ height: '100vh' }} verticalAlign='middle'>
                <Grid.Column style={{ maxWidth: 372 }}>
                    <Header as='h2' textAlign='center'>
                        Soulseek Web Example
                    </Header>
                    <Form size='large'>
                        <Segment>
                            <Form.Input 
                                fluid icon='user' 
                                iconPosition='left' 
                                placeholder='Username' 
                                onChange={(event) => this.handleChange('username', event.target.value)}
                            />
                            <Form.Input
                                fluid
                                icon='lock'
                                iconPosition='left'
                                placeholder='Password'
                                type='password'
                                onChange={(event) => this.handleChange('password', event.target.value)}
                            />
                            <Checkbox
                                label='Remember Me'
                                onChange={() => this.handleChange('rememberMe', !rememberMe)}
                                checked={rememberMe}
                            />
                        </Segment>
                        <Button 
                                primary 
                                fluid 
                                size='large'
                                className='login-button'
                                onClick={() => onLoginAttempt(username, password, rememberMe)}
                            >
                                <Icon name='sign in'/>
                                Login
                            </Button>
                    </Form>
                </Grid.Column>
            </Grid>
        )
    }
}

export default LoginForm